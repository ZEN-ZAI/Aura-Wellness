package entities

import (
	"errors"
	"time"

	"github.com/google/uuid"
)

type ChatConversation struct {
	ID          uuid.UUID `json:"id"`
	WorkspaceID uuid.UUID `json:"workspace_id"`
	Type        string    `json:"type"` // "group" or "dm"
	CreatedAt   time.Time `json:"created_at"`
}

type ChatConversationParticipant struct {
	ID             uuid.UUID `json:"id"`
	ConversationID uuid.UUID `json:"conversation_id"`
	PersonID       uuid.UUID `json:"person_id"`
	CreatedAt      time.Time `json:"created_at"`
}

func NewConversation(workspaceID uuid.UUID, convType string) (ChatConversation, error) {
	if workspaceID == uuid.Nil {
		return ChatConversation{}, errors.New("workspaceID is required")
	}
	if convType != "group" && convType != "dm" {
		return ChatConversation{}, errors.New("type must be 'group' or 'dm'")
	}
	return ChatConversation{
		ID:          uuid.New(),
		WorkspaceID: workspaceID,
		Type:        convType,
		CreatedAt:   time.Now().UTC(),
	}, nil
}

func NewConversationParticipant(conversationID, personID uuid.UUID) (ChatConversationParticipant, error) {
	if conversationID == uuid.Nil {
		return ChatConversationParticipant{}, errors.New("conversationID is required")
	}
	if personID == uuid.Nil {
		return ChatConversationParticipant{}, errors.New("personID is required")
	}
	return ChatConversationParticipant{
		ID:             uuid.New(),
		ConversationID: conversationID,
		PersonID:       personID,
		CreatedAt:      time.Now().UTC(),
	}, nil
}
