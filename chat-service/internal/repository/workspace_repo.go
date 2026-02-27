package repository

import (
	"context"
	"fmt"

	"github.com/aura-wellness/chat-service/internal/models"
	"github.com/google/uuid"
	"github.com/jackc/pgx/v5/pgxpool"
)

type WorkspaceRepo struct {
	pool *pgxpool.Pool
}

func NewWorkspaceRepo(pool *pgxpool.Pool) *WorkspaceRepo {
	return &WorkspaceRepo{pool: pool}
}

func (r *WorkspaceRepo) Create(ctx context.Context, buID, companyID uuid.UUID, name string) (*models.ChatWorkspace, error) {
	row := r.pool.QueryRow(ctx,
		`INSERT INTO chat_workspaces (bu_id, company_id, name) VALUES ($1, $2, $3) RETURNING id, bu_id, company_id, name, created_at`,
		buID, companyID, name,
	)
	var w models.ChatWorkspace
	if err := row.Scan(&w.ID, &w.BuID, &w.CompanyID, &w.Name, &w.CreatedAt); err != nil {
		return nil, fmt.Errorf("create workspace: %w", err)
	}
	return &w, nil
}

func (r *WorkspaceRepo) GetByBuID(ctx context.Context, buID uuid.UUID) (*models.ChatWorkspace, error) {
	row := r.pool.QueryRow(ctx,
		`SELECT id, bu_id, company_id, name, created_at FROM chat_workspaces WHERE bu_id = $1`,
		buID,
	)
	var w models.ChatWorkspace
	if err := row.Scan(&w.ID, &w.BuID, &w.CompanyID, &w.Name, &w.CreatedAt); err != nil {
		return nil, err
	}
	return &w, nil
}

func (r *WorkspaceRepo) GetByID(ctx context.Context, id uuid.UUID) (*models.ChatWorkspace, error) {
	row := r.pool.QueryRow(ctx,
		`SELECT id, bu_id, company_id, name, created_at FROM chat_workspaces WHERE id = $1`,
		id,
	)
	var w models.ChatWorkspace
	if err := row.Scan(&w.ID, &w.BuID, &w.CompanyID, &w.Name, &w.CreatedAt); err != nil {
		return nil, err
	}
	return &w, nil
}
