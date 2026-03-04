package ws

import (
	"net/http"
	"strings"

	"github.com/aura-wellness/chat-service/internal/domain/ports"
)

// NewServer builds an *http.Server that exposes GET /ws/{buId}.
// addr should be in the form ":8080".
func NewServer(
	workspaceSvc ports.WorkspaceService,
	messagingSvc ports.MessagingService,
	jwtSecret string,
	addr string,
) *http.Server {
	h := NewHandler(workspaceSvc, messagingSvc, jwtSecret)

	mux := http.NewServeMux()

	// Pattern: /ws/{buId}  (no trailing slash)
	mux.HandleFunc("/ws/", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodGet {
			http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
			return
		}
		// Extract buId: path is always /ws/<uuid>
		buIDStr := strings.TrimPrefix(r.URL.Path, "/ws/")
		if buIDStr == "" {
			http.Error(w, "missing buId", http.StatusBadRequest)
			return
		}
		h.ServeWS(w, r, buIDStr)
	})

	return &http.Server{
		Addr:    addr,
		Handler: mux,
	}
}
