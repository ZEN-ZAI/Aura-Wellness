package mocks

import (
	"context"
	"time"

	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/google/uuid"
)

// MockMessageRepository is a hand-written test double for ports.MessageRepository.
type MockMessageRepository struct {
	SaveFn func(ctx context.Context, m entities.ChatMessage) (entities.ChatMessage, error)
	ListFn func(ctx context.Context, workspaceID uuid.UUID, before time.Time, limit int) ([]entities.ChatMessage, error)
}

func (m *MockMessageRepository) Save(ctx context.Context, msg entities.ChatMessage) (entities.ChatMessage, error) {
	return m.SaveFn(ctx, msg)
}

func (m *MockMessageRepository) List(ctx context.Context, workspaceID uuid.UUID, before time.Time, limit int) ([]entities.ChatMessage, error) {
	return m.ListFn(ctx, workspaceID, before, limit)
}
