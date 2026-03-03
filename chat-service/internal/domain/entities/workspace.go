package entities

import (
	"errors"
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

func NewWorkspace(buID, companyID uuid.UUID, name string) (ChatWorkspace, error) {
	if buID == uuid.Nil {
		return ChatWorkspace{}, errors.New("buID is required")
	}
	if companyID == uuid.Nil {
		return ChatWorkspace{}, errors.New("companyID is required")
	}
	if name == "" {
		return ChatWorkspace{}, errors.New("name is required")
	}
	return ChatWorkspace{
		ID:        uuid.New(),
		BuID:      buID,
		CompanyID: companyID,
		Name:      name,
		CreatedAt: time.Now().UTC(),
	}, nil
}
