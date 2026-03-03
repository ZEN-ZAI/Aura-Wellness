package handlers_test

import (
	"context"
	"testing"
	"time"

	"github.com/aura-wellness/chat-service/internal/application"
	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/aura-wellness/chat-service/internal/transport/grpc/handlers"
	"github.com/aura-wellness/chat-service/internal/pb"
	"github.com/google/uuid"
	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"
)

// ── Mock MessagingService ─────────────────────────────────────────────────────

type mockMessagingSvc struct {
	SendMessageFn   func(ctx context.Context, workspaceID, personID uuid.UUID, senderName, content string) (entities.ChatMessage, error)
	ListMessagesFn  func(ctx context.Context, workspaceID uuid.UUID, before time.Time, limit int) ([]entities.ChatMessage, error)
	StreamMessagesFn func(ctx context.Context, workspaceID uuid.UUID) (<-chan entities.ChatMessage, func(), error)
}

func (m *mockMessagingSvc) SendMessage(ctx context.Context, wsID, pID uuid.UUID, senderName, content string) (entities.ChatMessage, error) {
	return m.SendMessageFn(ctx, wsID, pID, senderName, content)
}
func (m *mockMessagingSvc) ListMessages(ctx context.Context, wsID uuid.UUID, before time.Time, limit int) ([]entities.ChatMessage, error) {
	return m.ListMessagesFn(ctx, wsID, before, limit)
}
func (m *mockMessagingSvc) StreamMessages(ctx context.Context, wsID uuid.UUID) (<-chan entities.ChatMessage, func(), error) {
	return m.StreamMessagesFn(ctx, wsID)
}

// ── SendMessage ───────────────────────────────────────────────────────────────

func TestSendMessage_InvalidWorkspaceUUID_ReturnsInvalidArgument(t *testing.T) {
	h := handlers.NewMessagingHandler(&mockMessagingSvc{})
	_, err := h.SendMessage(context.Background(), &pb.SendMessageRequest{
		WorkspaceId: "not-a-uuid",
		PersonId:    uuid.New().String(),
		SenderName:  "Alice",
		Content:     "Hello",
	})

	require.Error(t, err)
	assert.Equal(t, codes.InvalidArgument, status.Code(err))
}

func TestSendMessage_InvalidPersonUUID_ReturnsInvalidArgument(t *testing.T) {
	h := handlers.NewMessagingHandler(&mockMessagingSvc{})
	_, err := h.SendMessage(context.Background(), &pb.SendMessageRequest{
		WorkspaceId: uuid.New().String(),
		PersonId:    "not-a-uuid",
		SenderName:  "Alice",
		Content:     "Hello",
	})

	assert.Equal(t, codes.InvalidArgument, status.Code(err))
}

func TestSendMessage_AccessDenied_ReturnsPermissionDenied(t *testing.T) {
	h := handlers.NewMessagingHandler(&mockMessagingSvc{
		SendMessageFn: func(_ context.Context, _, _ uuid.UUID, _, _ string) (entities.ChatMessage, error) {
			return entities.ChatMessage{}, application.ErrChatAccessDenied
		},
	})

	_, err := h.SendMessage(context.Background(), &pb.SendMessageRequest{
		WorkspaceId: uuid.New().String(),
		PersonId:    uuid.New().String(),
		SenderName:  "Alice",
		Content:     "Hello",
	})

	assert.Equal(t, codes.PermissionDenied, status.Code(err))
}

func TestSendMessage_Valid_ReturnsMappedChatMessage(t *testing.T) {
	msgID := uuid.New()
	wsID := uuid.New()
	pID := uuid.New()
	msg := entities.ChatMessage{
		ID:          msgID,
		WorkspaceID: wsID,
		PersonID:    pID,
		SenderName:  "Alice",
		Content:     "Hello!",
		CreatedAt:   time.Now().UTC(),
	}

	h := handlers.NewMessagingHandler(&mockMessagingSvc{
		SendMessageFn: func(_ context.Context, _, _ uuid.UUID, _, _ string) (entities.ChatMessage, error) {
			return msg, nil
		},
	})

	resp, err := h.SendMessage(context.Background(), &pb.SendMessageRequest{
		WorkspaceId: wsID.String(),
		PersonId:    pID.String(),
		SenderName:  "Alice",
		Content:     "Hello!",
	})

	require.NoError(t, err)
	assert.Equal(t, msgID.String(), resp.Id)
	assert.Equal(t, "Hello!", resp.Content)
	assert.Equal(t, "Alice", resp.SenderName)
}

// ── ListMessages ──────────────────────────────────────────────────────────────

func TestListMessages_InvalidWorkspaceUUID_ReturnsInvalidArgument(t *testing.T) {
	h := handlers.NewMessagingHandler(&mockMessagingSvc{})
	_, err := h.ListMessages(context.Background(), &pb.ListMessagesRequest{
		WorkspaceId: "bad-uuid",
	})

	assert.Equal(t, codes.InvalidArgument, status.Code(err))
}

func TestListMessages_ZeroLimit_DefaultsTo50(t *testing.T) {
	capturedLimit := 0
	wsID := uuid.New()

	h := handlers.NewMessagingHandler(&mockMessagingSvc{
		ListMessagesFn: func(_ context.Context, _ uuid.UUID, _ time.Time, limit int) ([]entities.ChatMessage, error) {
			capturedLimit = limit
			return []entities.ChatMessage{}, nil
		},
	})

	_, err := h.ListMessages(context.Background(), &pb.ListMessagesRequest{
		WorkspaceId: wsID.String(),
		Limit:       0, // should default to 50
	})

	require.NoError(t, err)
	assert.Equal(t, 50, capturedLimit)
}

func TestListMessages_OverMaxLimit_DefaultsTo50(t *testing.T) {
	capturedLimit := 0

	h := handlers.NewMessagingHandler(&mockMessagingSvc{
		ListMessagesFn: func(_ context.Context, _ uuid.UUID, _ time.Time, limit int) ([]entities.ChatMessage, error) {
			capturedLimit = limit
			return []entities.ChatMessage{}, nil
		},
	})

	_, err := h.ListMessages(context.Background(), &pb.ListMessagesRequest{
		WorkspaceId: uuid.New().String(),
		Limit:       201, // over max
	})

	require.NoError(t, err)
	assert.Equal(t, 50, capturedLimit)
}

func TestListMessages_Valid_ReturnsMappedMessages(t *testing.T) {
	wsID := uuid.New()
	msgs := []entities.ChatMessage{
		{ID: uuid.New(), Content: "Hello", CreatedAt: time.Now()},
		{ID: uuid.New(), Content: "World", CreatedAt: time.Now()},
	}

	h := handlers.NewMessagingHandler(&mockMessagingSvc{
		ListMessagesFn: func(_ context.Context, _ uuid.UUID, _ time.Time, _ int) ([]entities.ChatMessage, error) {
			return msgs, nil
		},
	})

	resp, err := h.ListMessages(context.Background(), &pb.ListMessagesRequest{
		WorkspaceId: wsID.String(),
		Limit:       10,
	})

	require.NoError(t, err)
	assert.Len(t, resp.Messages, 2)
	assert.Equal(t, "Hello", resp.Messages[0].Content)
}
