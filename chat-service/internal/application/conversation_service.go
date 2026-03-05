package application

import (
	"context"

	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/aura-wellness/chat-service/internal/domain/ports"
	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
)

type ConversationService struct {
	convRepo ports.ConversationRepository
}

func NewConversationService(cr ports.ConversationRepository) *ConversationService {
	return &ConversationService{convRepo: cr}
}

func (s *ConversationService) GetOrCreateGroupConversation(ctx context.Context, workspaceID uuid.UUID) (entities.ChatConversation, error) {
	conv, err := s.convRepo.GetGroupByWorkspace(ctx, workspaceID)
	if err == nil {
		return conv, nil
	}
	// If not found, create one
	if err.Error() != "get group conversation: "+pgx.ErrNoRows.Error() {
		return entities.ChatConversation{}, err
	}

	newConv, err := entities.NewConversation(workspaceID, "group")
	if err != nil {
		return entities.ChatConversation{}, err
	}
	return s.convRepo.Create(ctx, newConv)
}

func (s *ConversationService) GetOrCreateDMConversation(ctx context.Context, workspaceID, personA, personB uuid.UUID) (entities.ChatConversation, error) {
	// Try to find existing DM
	conv, err := s.convRepo.GetDMByParticipants(ctx, workspaceID, personA, personB)
	if err == nil {
		return conv, nil
	}

	// Create new DM conversation
	newConv, err := entities.NewConversation(workspaceID, "dm")
	if err != nil {
		return entities.ChatConversation{}, err
	}
	created, err := s.convRepo.Create(ctx, newConv)
	if err != nil {
		return entities.ChatConversation{}, err
	}

	// Add both participants
	pA, err := entities.NewConversationParticipant(created.ID, personA)
	if err != nil {
		return entities.ChatConversation{}, err
	}
	if err := s.convRepo.AddParticipant(ctx, pA); err != nil {
		return entities.ChatConversation{}, err
	}

	pB, err := entities.NewConversationParticipant(created.ID, personB)
	if err != nil {
		return entities.ChatConversation{}, err
	}
	if err := s.convRepo.AddParticipant(ctx, pB); err != nil {
		return entities.ChatConversation{}, err
	}

	return created, nil
}

func (s *ConversationService) ListConversations(ctx context.Context, workspaceID, personID uuid.UUID) ([]entities.ChatConversation, error) {
	return s.convRepo.ListByWorkspaceAndPerson(ctx, workspaceID, personID)
}

func (s *ConversationService) GetConversationParticipants(ctx context.Context, conversationID uuid.UUID) ([]entities.ChatConversationParticipant, error) {
	return s.convRepo.ListParticipants(ctx, conversationID)
}

func (s *ConversationService) GetByID(ctx context.Context, id uuid.UUID) (entities.ChatConversation, error) {
	return s.convRepo.GetByID(ctx, id)
}
