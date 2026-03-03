package mocks

import (
	"context"

	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/google/uuid"
)

// MockWorkspaceRepository is a hand-written test double for ports.WorkspaceRepository.
type MockWorkspaceRepository struct {
	CreateFn    func(ctx context.Context, w entities.ChatWorkspace) (entities.ChatWorkspace, error)
	GetByBuIDFn func(ctx context.Context, buID uuid.UUID) (entities.ChatWorkspace, error)
	GetByIDFn   func(ctx context.Context, id uuid.UUID) (entities.ChatWorkspace, error)
}

func (m *MockWorkspaceRepository) Create(ctx context.Context, w entities.ChatWorkspace) (entities.ChatWorkspace, error) {
	return m.CreateFn(ctx, w)
}

func (m *MockWorkspaceRepository) GetByBuID(ctx context.Context, buID uuid.UUID) (entities.ChatWorkspace, error) {
	return m.GetByBuIDFn(ctx, buID)
}

func (m *MockWorkspaceRepository) GetByID(ctx context.Context, id uuid.UUID) (entities.ChatWorkspace, error) {
	return m.GetByIDFn(ctx, id)
}
