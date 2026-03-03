package postgres

import (
	"context"
	"fmt"

	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/google/uuid"
	"github.com/jackc/pgx/v5/pgxpool"
)

type workspaceRepo struct {
	pool *pgxpool.Pool
}

func NewWorkspaceRepo(pool *pgxpool.Pool) *workspaceRepo {
	return &workspaceRepo{pool: pool}
}

func (r *workspaceRepo) Create(ctx context.Context, w entities.ChatWorkspace) (entities.ChatWorkspace, error) {
	row := r.pool.QueryRow(ctx,
		`INSERT INTO chat_workspaces (bu_id, company_id, name)
		 VALUES ($1, $2, $3)
		 RETURNING id, bu_id, company_id, name, created_at`,
		w.BuID, w.CompanyID, w.Name,
	)
	var out entities.ChatWorkspace
	if err := row.Scan(&out.ID, &out.BuID, &out.CompanyID, &out.Name, &out.CreatedAt); err != nil {
		return entities.ChatWorkspace{}, fmt.Errorf("create workspace: %w", err)
	}
	return out, nil
}

func (r *workspaceRepo) GetByBuID(ctx context.Context, buID uuid.UUID) (entities.ChatWorkspace, error) {
	row := r.pool.QueryRow(ctx,
		`SELECT id, bu_id, company_id, name, created_at FROM chat_workspaces WHERE bu_id = $1`,
		buID,
	)
	var w entities.ChatWorkspace
	if err := row.Scan(&w.ID, &w.BuID, &w.CompanyID, &w.Name, &w.CreatedAt); err != nil {
		return entities.ChatWorkspace{}, err
	}
	return w, nil
}

func (r *workspaceRepo) GetByID(ctx context.Context, id uuid.UUID) (entities.ChatWorkspace, error) {
	row := r.pool.QueryRow(ctx,
		`SELECT id, bu_id, company_id, name, created_at FROM chat_workspaces WHERE id = $1`,
		id,
	)
	var w entities.ChatWorkspace
	if err := row.Scan(&w.ID, &w.BuID, &w.CompanyID, &w.Name, &w.CreatedAt); err != nil {
		return entities.ChatWorkspace{}, err
	}
	return w, nil
}
