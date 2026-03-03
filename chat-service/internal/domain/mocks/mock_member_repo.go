package mocks

import (
	"context"

	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/google/uuid"
)

// MockMemberRepository is a hand-written test double for ports.MemberRepository.
type MockMemberRepository struct {
	UpsertFn                  func(ctx context.Context, m entities.ChatWorkspaceMember) (entities.ChatWorkspaceMember, error)
	UpdateAccessFn            func(ctx context.Context, workspaceID, personID uuid.UUID, hasAccess bool) (entities.ChatWorkspaceMember, error)
	ListByWorkspaceFn         func(ctx context.Context, workspaceID uuid.UUID) ([]entities.ChatWorkspaceMember, error)
	GetByWorkspaceAndPersonFn func(ctx context.Context, workspaceID, personID uuid.UUID) (entities.ChatWorkspaceMember, error)
}

func (m *MockMemberRepository) Upsert(ctx context.Context, member entities.ChatWorkspaceMember) (entities.ChatWorkspaceMember, error) {
	return m.UpsertFn(ctx, member)
}

func (m *MockMemberRepository) UpdateAccess(ctx context.Context, workspaceID, personID uuid.UUID, hasAccess bool) (entities.ChatWorkspaceMember, error) {
	return m.UpdateAccessFn(ctx, workspaceID, personID, hasAccess)
}

func (m *MockMemberRepository) ListByWorkspace(ctx context.Context, workspaceID uuid.UUID) ([]entities.ChatWorkspaceMember, error) {
	return m.ListByWorkspaceFn(ctx, workspaceID)
}

func (m *MockMemberRepository) GetByWorkspaceAndPerson(ctx context.Context, workspaceID, personID uuid.UUID) (entities.ChatWorkspaceMember, error) {
	return m.GetByWorkspaceAndPersonFn(ctx, workspaceID, personID)
}
