package repository

import (
	"context"
	"fmt"

	"github.com/aura-wellness/chat-service/internal/models"
	"github.com/google/uuid"
	"github.com/jackc/pgx/v5/pgxpool"
)

type MemberRepo struct {
	pool *pgxpool.Pool
}

func NewMemberRepo(pool *pgxpool.Pool) *MemberRepo {
	return &MemberRepo{pool: pool}
}

func (r *MemberRepo) Add(ctx context.Context, workspaceID, personID uuid.UUID, role string) (*models.ChatWorkspaceMember, error) {
	row := r.pool.QueryRow(ctx,
		`INSERT INTO chat_workspace_members (workspace_id, person_id, role, has_access)
		 VALUES ($1, $2, $3, $4)
		 ON CONFLICT (workspace_id, person_id) DO UPDATE SET role = EXCLUDED.role
		 RETURNING id, workspace_id, person_id, role, has_access, created_at`,
		workspaceID, personID, role, role == "Admin",
	)
	var m models.ChatWorkspaceMember
	if err := row.Scan(&m.ID, &m.WorkspaceID, &m.PersonID, &m.Role, &m.HasAccess, &m.CreatedAt); err != nil {
		return nil, fmt.Errorf("add member: %w", err)
	}
	return &m, nil
}

func (r *MemberRepo) UpdateAccess(ctx context.Context, workspaceID, personID uuid.UUID, hasAccess bool) (*models.ChatWorkspaceMember, error) {
	// Upsert: insert as Member if not exists, then update access
	row := r.pool.QueryRow(ctx,
		`INSERT INTO chat_workspace_members (workspace_id, person_id, role, has_access)
		 VALUES ($1, $2, 'Member', $3)
		 ON CONFLICT (workspace_id, person_id) DO UPDATE SET has_access = EXCLUDED.has_access
		 RETURNING id, workspace_id, person_id, role, has_access, created_at`,
		workspaceID, personID, hasAccess,
	)
	var m models.ChatWorkspaceMember
	if err := row.Scan(&m.ID, &m.WorkspaceID, &m.PersonID, &m.Role, &m.HasAccess, &m.CreatedAt); err != nil {
		return nil, fmt.Errorf("update access: %w", err)
	}
	return &m, nil
}

func (r *MemberRepo) ListByWorkspace(ctx context.Context, workspaceID uuid.UUID) ([]*models.ChatWorkspaceMember, error) {
	rows, err := r.pool.Query(ctx,
		`SELECT id, workspace_id, person_id, role, has_access, created_at
		 FROM chat_workspace_members WHERE workspace_id = $1 ORDER BY created_at`,
		workspaceID,
	)
	if err != nil {
		return nil, fmt.Errorf("list members: %w", err)
	}
	defer rows.Close()

	var members []*models.ChatWorkspaceMember
	for rows.Next() {
		var m models.ChatWorkspaceMember
		if err := rows.Scan(&m.ID, &m.WorkspaceID, &m.PersonID, &m.Role, &m.HasAccess, &m.CreatedAt); err != nil {
			return nil, err
		}
		members = append(members, &m)
	}
	return members, rows.Err()
}
