package postgres

import (
	"context"
	"fmt"

	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/google/uuid"
	"github.com/jackc/pgx/v5/pgxpool"
)

type memberRepo struct {
	pool *pgxpool.Pool
}

func NewMemberRepo(pool *pgxpool.Pool) *memberRepo {
	return &memberRepo{pool: pool}
}

// Upsert inserts a member or updates the role on conflict.
// has_access comes from the entity (set by entities.NewMember business rule).
func (r *memberRepo) Upsert(ctx context.Context, m entities.ChatWorkspaceMember) (entities.ChatWorkspaceMember, error) {
	row := r.pool.QueryRow(ctx,
		`INSERT INTO chat_workspace_members (workspace_id, person_id, role, has_access)
		 VALUES ($1, $2, $3, $4)
		 ON CONFLICT (workspace_id, person_id) DO UPDATE SET role = EXCLUDED.role
		 RETURNING id, workspace_id, person_id, role, has_access, created_at`,
		m.WorkspaceID, m.PersonID, m.Role, m.HasAccess,
	)
	var out entities.ChatWorkspaceMember
	if err := row.Scan(&out.ID, &out.WorkspaceID, &out.PersonID, &out.Role, &out.HasAccess, &out.CreatedAt); err != nil {
		return entities.ChatWorkspaceMember{}, fmt.Errorf("upsert member: %w", err)
	}
	return out, nil
}

// UpdateAccess sets has_access for an existing member, or inserts as Member role if not found.
func (r *memberRepo) UpdateAccess(ctx context.Context, workspaceID, personID uuid.UUID, hasAccess bool) (entities.ChatWorkspaceMember, error) {
	row := r.pool.QueryRow(ctx,
		`INSERT INTO chat_workspace_members (workspace_id, person_id, role, has_access)
		 VALUES ($1, $2, 'Member', $3)
		 ON CONFLICT (workspace_id, person_id) DO UPDATE SET has_access = EXCLUDED.has_access
		 RETURNING id, workspace_id, person_id, role, has_access, created_at`,
		workspaceID, personID, hasAccess,
	)
	var m entities.ChatWorkspaceMember
	if err := row.Scan(&m.ID, &m.WorkspaceID, &m.PersonID, &m.Role, &m.HasAccess, &m.CreatedAt); err != nil {
		return entities.ChatWorkspaceMember{}, fmt.Errorf("update access: %w", err)
	}
	return m, nil
}

func (r *memberRepo) ListByWorkspace(ctx context.Context, workspaceID uuid.UUID) ([]entities.ChatWorkspaceMember, error) {
	rows, err := r.pool.Query(ctx,
		`SELECT id, workspace_id, person_id, role, has_access, created_at
		 FROM chat_workspace_members WHERE workspace_id = $1 ORDER BY created_at`,
		workspaceID,
	)
	if err != nil {
		return nil, fmt.Errorf("list members: %w", err)
	}
	defer rows.Close()

	var members []entities.ChatWorkspaceMember
	for rows.Next() {
		var m entities.ChatWorkspaceMember
		if err := rows.Scan(&m.ID, &m.WorkspaceID, &m.PersonID, &m.Role, &m.HasAccess, &m.CreatedAt); err != nil {
			return nil, err
		}
		members = append(members, m)
	}
	return members, rows.Err()
}

// GetByWorkspaceAndPerson looks up a single member — used by MessagingService to check access.
func (r *memberRepo) GetByWorkspaceAndPerson(ctx context.Context, workspaceID, personID uuid.UUID) (entities.ChatWorkspaceMember, error) {
	row := r.pool.QueryRow(ctx,
		`SELECT id, workspace_id, person_id, role, has_access, created_at
		 FROM chat_workspace_members WHERE workspace_id = $1 AND person_id = $2`,
		workspaceID, personID,
	)
	var m entities.ChatWorkspaceMember
	if err := row.Scan(&m.ID, &m.WorkspaceID, &m.PersonID, &m.Role, &m.HasAccess, &m.CreatedAt); err != nil {
		return entities.ChatWorkspaceMember{}, fmt.Errorf("get member: %w", err)
	}
	return m, nil
}
