package entities

import (
	"errors"
	"time"

	"github.com/google/uuid"
)

type ChatMessage struct {
	ID             uuid.UUID `json:"id"`
	WorkspaceID    uuid.UUID `json:"workspace_id"`
	ConversationID uuid.UUID `json:"conversation_id"`
	PersonID       uuid.UUID `json:"person_id"`
	SenderName     string    `json:"sender_name"`
	Content        string    `json:"content"`
	CreatedAt      time.Time `json:"created_at"`
}

func NewMessage(workspaceID, conversationID, personID uuid.UUID, senderName, content string) (ChatMessage, error) {
	if workspaceID == uuid.Nil {
		return ChatMessage{}, errors.New("workspaceID is required")
	}
	if conversationID == uuid.Nil {
		return ChatMessage{}, errors.New("conversationID is required")
	}
	if personID == uuid.Nil {
		return ChatMessage{}, errors.New("personID is required")
	}
	if content == "" {
		return ChatMessage{}, errors.New("content is required")
	}
	return ChatMessage{
		ID:             uuid.New(),
		WorkspaceID:    workspaceID,
		ConversationID: conversationID,
		PersonID:       personID,
		SenderName:     senderName,
		Content:        content,
		CreatedAt:      time.Now().UTC(),
	}, nil
}
