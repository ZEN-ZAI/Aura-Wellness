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
	SendMessage(ctx context.Context, workspaceID, conversationID, personID uuid.UUID, senderName, content string) (entities.ChatMessage, error)
	ListMessages(ctx context.Context, conversationID uuid.UUID, before time.Time, limit int) ([]entities.ChatMessage, error)
	StreamMessages(ctx context.Context, conversationID uuid.UUID) (<-chan entities.ChatMessage, func(), error)
}

type ConversationService interface {
	GetOrCreateGroupConversation(ctx context.Context, workspaceID uuid.UUID) (entities.ChatConversation, error)
	GetOrCreateDMConversation(ctx context.Context, workspaceID, personA, personB uuid.UUID) (entities.ChatConversation, error)
	ListConversations(ctx context.Context, workspaceID, personID uuid.UUID) ([]entities.ChatConversation, error)
	GetConversationParticipants(ctx context.Context, conversationID uuid.UUID) ([]entities.ChatConversationParticipant, error)
	GetByID(ctx context.Context, id uuid.UUID) (entities.ChatConversation, error)
}
