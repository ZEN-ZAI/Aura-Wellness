package ports

import (
	"context"
	"time"

	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/google/uuid"
)

type WorkspaceService interface {
	CreateWorkspace(ctx context.Context, buID, companyID uuid.UUID, name string) (entities.ChatWorkspace, error)
	GetByBuID(ctx context.Context, buID uuid.UUID) (entities.ChatWorkspace, error)
	GetByID(ctx context.Context, id uuid.UUID) (entities.ChatWorkspace, error)
	AddMember(ctx context.Context, workspaceID, personID uuid.UUID, role string) (entities.ChatWorkspaceMember, error)
	UpdateMemberAccess(ctx context.Context, workspaceID, personID uuid.UUID, hasAccess bool) (entities.ChatWorkspaceMember, error)
	ListMembers(ctx context.Context, workspaceID uuid.UUID) ([]entities.ChatWorkspaceMember, error)
}

type MessagingService interface {
	SendMessage(ctx context.Context, workspaceID, personID uuid.UUID, senderName, content string) (entities.ChatMessage, error)
	ListMessages(ctx context.Context, workspaceID uuid.UUID, before time.Time, limit int) ([]entities.ChatMessage, error)
	StreamMessages(ctx context.Context, workspaceID uuid.UUID) (<-chan entities.ChatMessage, func(), error)
}
