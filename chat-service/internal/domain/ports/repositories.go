package ports

import (
	"context"
	"time"

	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/google/uuid"
)

type WorkspaceRepository interface {
	Create(ctx context.Context, w entities.ChatWorkspace) (entities.ChatWorkspace, error)
	GetByBuID(ctx context.Context, buID uuid.UUID) (entities.ChatWorkspace, error)
	GetByID(ctx context.Context, id uuid.UUID) (entities.ChatWorkspace, error)
}

type MemberRepository interface {
	Upsert(ctx context.Context, m entities.ChatWorkspaceMember) (entities.ChatWorkspaceMember, error)
	UpdateAccess(ctx context.Context, workspaceID, personID uuid.UUID, hasAccess bool) (entities.ChatWorkspaceMember, error)
	ListByWorkspace(ctx context.Context, workspaceID uuid.UUID) ([]entities.ChatWorkspaceMember, error)
	GetByWorkspaceAndPerson(ctx context.Context, workspaceID, personID uuid.UUID) (entities.ChatWorkspaceMember, error)
}

type MessageRepository interface {
	Save(ctx context.Context, m entities.ChatMessage) (entities.ChatMessage, error)
	List(ctx context.Context, conversationID uuid.UUID, before time.Time, limit int) ([]entities.ChatMessage, error)
}

type ConversationRepository interface {
	Create(ctx context.Context, c entities.ChatConversation) (entities.ChatConversation, error)
	GetByID(ctx context.Context, id uuid.UUID) (entities.ChatConversation, error)
	GetGroupByWorkspace(ctx context.Context, workspaceID uuid.UUID) (entities.ChatConversation, error)
	GetDMByParticipants(ctx context.Context, workspaceID, personA, personB uuid.UUID) (entities.ChatConversation, error)
	ListByWorkspaceAndPerson(ctx context.Context, workspaceID, personID uuid.UUID) ([]entities.ChatConversation, error)
	AddParticipant(ctx context.Context, p entities.ChatConversationParticipant) error
	ListParticipants(ctx context.Context, conversationID uuid.UUID) ([]entities.ChatConversationParticipant, error)
}
