package ws

import (
	"context"
	"encoding/json"
	"log"
	"net/http"
	"sync"
	"time"

	"github.com/google/uuid"
	"github.com/gorilla/websocket"

	"github.com/aura-wellness/chat-service/internal/domain/ports"
)

var upgrader = websocket.Upgrader{
	HandshakeTimeout: 10 * time.Second,
	// Allow all origins — the Next.js server.ts proxy is the only caller, and
	// it already validates the JWT before forwarding the connection.
	CheckOrigin: func(r *http.Request) bool { return true },
}

// outboundMessage is the JSON frame sent to the browser for each chat message.
type outboundMessage struct {
	ID             string    `json:"id"`
	ConversationID string    `json:"conversationId"`
	PersonID       string    `json:"personId"`
	SenderName     string    `json:"senderName"`
	Content        string    `json:"content"`
	CreatedAt      time.Time `json:"createdAt"`
}

// inboundMessage is the expected shape of frames coming from the browser.
type inboundMessage struct {
	Type           string `json:"type"`           // "send_message"
	ConversationID string `json:"conversationId"` // target conversation
	Content        string `json:"content"`
}

// Handler handles WebSocket connections for a single workspace.
type Handler struct {
	workspaceSvc    ports.WorkspaceService
	messagingSvc    ports.MessagingService
	conversationSvc ports.ConversationService
	jwtSecret       string
}

func NewHandler(wsSvc ports.WorkspaceService, msgSvc ports.MessagingService, convSvc ports.ConversationService, jwtSecret string) *Handler {
	return &Handler{
		workspaceSvc:    wsSvc,
		messagingSvc:    msgSvc,
		conversationSvc: convSvc,
		jwtSecret:       jwtSecret,
	}
}

// ServeWS is the http.HandlerFunc for GET /ws/{buId}.
// buId is extracted from the URL by the server router and passed in directly.
func (h *Handler) ServeWS(w http.ResponseWriter, r *http.Request, buIDStr string) {
	// ── 1. Parse and validate JWT ────────────────────────────────────────────
	claims, status, err := ParseClaims(r, h.jwtSecret)
	if err != nil {
		http.Error(w, err.Error(), status)
		return
	}

	// ── 2. Verify buId in URL matches the token claim (anti-impersonation) ───
	buID, err := uuid.Parse(buIDStr)
	if err != nil {
		http.Error(w, "invalid buId", http.StatusBadRequest)
		return
	}
	if claims.BuID != buID {
		http.Error(w, "forbidden: buId mismatch", http.StatusForbidden)
		return
	}

	// ── 3. Resolve workspace ─────────────────────────────────────────────────
	ctx := r.Context()
	ws, err := h.workspaceSvc.GetByBuID(ctx, buID)
	if err != nil {
		http.Error(w, "workspace not found", http.StatusNotFound)
		return
	}

	// ── 4. Fetch conversations this user participates in ─────────────────────
	conversations, err := h.conversationSvc.ListConversations(ctx, ws.ID, claims.PersonID)
	if err != nil {
		http.Error(w, "failed to load conversations", http.StatusInternalServerError)
		return
	}

	// ── 5. Upgrade the HTTP connection to WebSocket ──────────────────────────
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Printf("[ws] upgrade error buId=%s: %v", buIDStr, err)
		return
	}
	defer conn.Close()

	log.Printf("[ws] connected personId=%s buId=%s conversations=%d", claims.PersonID, buIDStr, len(conversations))

	// ── 6. Subscribe to ALL conversations (write pump) ───────────────────────
	streamCtx, cancelStream := context.WithCancel(ctx)
	defer cancelStream()

	// Mutex protects concurrent writes to the WebSocket connection.
	var writeMu sync.Mutex

	// Track cleanup functions for all subscriptions.
	var cleanups []func()
	defer func() {
		for _, fn := range cleanups {
			fn()
		}
	}()

	writeDone := make(chan struct{})
	var wg sync.WaitGroup

	for _, conv := range conversations {
		msgCh, cleanup, err := h.messagingSvc.StreamMessages(streamCtx, conv.ID)
		if err != nil {
			log.Printf("[ws] stream error convId=%s: %v", conv.ID, err)
			continue
		}
		cleanups = append(cleanups, cleanup)
		convID := conv.ID.String()

		wg.Add(1)
		go func() {
			defer wg.Done()
			for msg := range msgCh {
				frame := outboundMessage{
					ID:             msg.ID.String(),
					ConversationID: convID,
					PersonID:       msg.PersonID.String(),
					SenderName:     msg.SenderName,
					Content:        msg.Content,
					CreatedAt:      msg.CreatedAt,
				}
				writeMu.Lock()
				writeErr := conn.WriteJSON(frame)
				writeMu.Unlock()
				if writeErr != nil {
					log.Printf("[ws] write error: %v", writeErr)
					return
				}
			}
		}()
	}

	go func() {
		wg.Wait()
		close(writeDone)
	}()

	// ── 7. Read pump: handle incoming frames from the browser ────────────────
	conn.SetReadLimit(4096) // 4 KB max per frame
	conn.SetReadDeadline(time.Now().Add(60 * time.Second))
	conn.SetPongHandler(func(string) error {
		conn.SetReadDeadline(time.Now().Add(60 * time.Second))
		return nil
	})

	// Ping the client every 30 s so the connection stays alive.
	pingTicker := time.NewTicker(30 * time.Second)
	defer pingTicker.Stop()

	go func() {
		for range pingTicker.C {
			writeMu.Lock()
			pingErr := conn.WriteMessage(websocket.PingMessage, nil)
			writeMu.Unlock()
			if pingErr != nil {
				return
			}
		}
	}()

	for {
		_, rawMsg, err := conn.ReadMessage()
		if err != nil {
			if websocket.IsUnexpectedCloseError(err, websocket.CloseGoingAway, websocket.CloseNormalClosure) {
				log.Printf("[ws] read error personId=%s: %v", claims.PersonID, err)
			}
			cancelStream()
			break
		}
		conn.SetReadDeadline(time.Now().Add(60 * time.Second))

		var inbound inboundMessage
		if err := json.Unmarshal(rawMsg, &inbound); err != nil || inbound.Type != "send_message" {
			continue // ignore unknown frame types
		}

		convID, err := uuid.Parse(inbound.ConversationID)
		if err != nil {
			log.Printf("[ws] invalid conversationId from client: %v", err)
			continue
		}

		if _, err := h.messagingSvc.SendMessage(
			ctx, ws.ID, convID, claims.PersonID, claims.SenderName, inbound.Content,
		); err != nil {
			log.Printf("[ws] send message error: %v", err)
		}
	}

	<-writeDone
	log.Printf("[ws] disconnected personId=%s buId=%s", claims.PersonID, buIDStr)
}
