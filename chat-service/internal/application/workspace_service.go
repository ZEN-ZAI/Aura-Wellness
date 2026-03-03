package application

import (
	"context"

	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/aura-wellness/chat-service/internal/domain/ports"
	"github.com/google/uuid"
)

type WorkspaceService struct {
	workspaceRepo ports.WorkspaceRepository
	memberRepo    ports.MemberRepository
}

func NewWorkspaceService(wr ports.WorkspaceRepository, mr ports.MemberRepository) *WorkspaceService {
	return &WorkspaceService{workspaceRepo: wr, memberRepo: mr}
}

func (s *WorkspaceService) CreateWorkspace(ctx context.Context, buID, companyID uuid.UUID, name string) (entities.ChatWorkspace, error) {
	w, err := entities.NewWorkspace(buID, companyID, name)
	if err != nil {
		return entities.ChatWorkspace{}, err
	}
	return s.workspaceRepo.Create(ctx, w)
}

func (s *WorkspaceService) GetByBuID(ctx context.Context, buID uuid.UUID) (entities.ChatWorkspace, error) {
	return s.workspaceRepo.GetByBuID(ctx, buID)
}

func (s *WorkspaceService) GetByID(ctx context.Context, id uuid.UUID) (entities.ChatWorkspace, error) {
	return s.workspaceRepo.GetByID(ctx, id)
}

// AddMember creates or updates a workspace member. The has_access business rule
// (Admins get access by default) is encoded in entities.NewMember.
func (s *WorkspaceService) AddMember(ctx context.Context, workspaceID, personID uuid.UUID, role string) (entities.ChatWorkspaceMember, error) {
	m, err := entities.NewMember(workspaceID, personID, role)
	if err != nil {
		return entities.ChatWorkspaceMember{}, err
	}
	return s.memberRepo.Upsert(ctx, m)
}

func (s *WorkspaceService) UpdateMemberAccess(ctx context.Context, workspaceID, personID uuid.UUID, hasAccess bool) (entities.ChatWorkspaceMember, error) {
	return s.memberRepo.UpdateAccess(ctx, workspaceID, personID, hasAccess)
}

func (s *WorkspaceService) ListMembers(ctx context.Context, workspaceID uuid.UUID) ([]entities.ChatWorkspaceMember, error) {
	return s.memberRepo.ListByWorkspace(ctx, workspaceID)
}
