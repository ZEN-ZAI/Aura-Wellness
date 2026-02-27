package models

import (
	"time"

	"github.com/google/uuid"
)

type ChatWorkspace struct {
	ID        uuid.UUID `json:"id"`
	BuID      uuid.UUID `json:"bu_id"`
	CompanyID uuid.UUID `json:"company_id"`
	Name      string    `json:"name"`
	CreatedAt time.Time `json:"created_at"`
}

type ChatWorkspaceMember struct {
	ID          uuid.UUID `json:"id"`
	WorkspaceID uuid.UUID `json:"workspace_id"`
	PersonID    uuid.UUID `json:"person_id"`
	Role        string    `json:"role"`
	HasAccess   bool      `json:"has_access"`
	CreatedAt   time.Time `json:"created_at"`
}
