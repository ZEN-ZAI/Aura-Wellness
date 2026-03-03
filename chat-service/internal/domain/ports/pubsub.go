package ports

import (
	"context"

	"github.com/aura-wellness/chat-service/internal/domain/entities"
)

// MessageEvent is the unit published to and received from a Pub/Sub channel.
type MessageEvent struct {
	Message entities.ChatMessage `json:"message"`
}

// PubSub abstracts the real-time message broadcasting mechanism.
// Subscribe returns a receive-only channel of MessageEvents and a cleanup function
// that must be called when the subscriber is done.
type PubSub interface {
	Publish(ctx context.Context, workspaceID string, event MessageEvent) error
	Subscribe(ctx context.Context, workspaceID string) (<-chan MessageEvent, func(), error)
}
