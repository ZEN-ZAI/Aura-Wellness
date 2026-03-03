package entities

import (
	"errors"
	"time"

	"github.com/google/uuid"
)

type ChatWorkspaceMember struct {
	ID          uuid.UUID `json:"id"`
	WorkspaceID uuid.UUID `json:"workspace_id"`
	PersonID    uuid.UUID `json:"person_id"`
	Role        string    `json:"role"`
	HasAccess   bool      `json:"has_access"`
	CreatedAt   time.Time `json:"created_at"`
}

// NewMember constructs a member. The business rule "Admins get chat access by default"
// lives here rather than being scattered across SQL and handler code.
func NewMember(workspaceID, personID uuid.UUID, role string) (ChatWorkspaceMember, error) {
	if workspaceID == uuid.Nil {
		return ChatWorkspaceMember{}, errors.New("workspaceID is required")
	}
	if personID == uuid.Nil {
		return ChatWorkspaceMember{}, errors.New("personID is required")
	}
	if role == "" {
		return ChatWorkspaceMember{}, errors.New("role is required")
	}
	return ChatWorkspaceMember{
		ID:          uuid.New(),
		WorkspaceID: workspaceID,
		PersonID:    personID,
		Role:        role,
		HasAccess:   role == "Admin",
		CreatedAt:   time.Now().UTC(),
	}, nil
}
