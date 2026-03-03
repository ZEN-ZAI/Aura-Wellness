package postgres

import (
	"context"
	"fmt"
	"time"

	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/google/uuid"
	"github.com/jackc/pgx/v5/pgxpool"
)

type messageRepo struct {
	pool *pgxpool.Pool
}

func NewMessageRepo(pool *pgxpool.Pool) *messageRepo {
	return &messageRepo{pool: pool}
}

func (r *messageRepo) Save(ctx context.Context, m entities.ChatMessage) (entities.ChatMessage, error) {
	row := r.pool.QueryRow(ctx,
		`INSERT INTO chat_messages (workspace_id, person_id, sender_name, content)
		 VALUES ($1, $2, $3, $4)
		 RETURNING id, workspace_id, person_id, sender_name, content, created_at`,
		m.WorkspaceID, m.PersonID, m.SenderName, m.Content,
	)
	var out entities.ChatMessage
	if err := row.Scan(&out.ID, &out.WorkspaceID, &out.PersonID, &out.SenderName, &out.Content, &out.CreatedAt); err != nil {
		return entities.ChatMessage{}, fmt.Errorf("save message: %w", err)
	}
	return out, nil
}

// List returns messages oldest-first, paginated by a before-timestamp cursor.
func (r *messageRepo) List(ctx context.Context, workspaceID uuid.UUID, before time.Time, limit int) ([]entities.ChatMessage, error) {
	rows, err := r.pool.Query(ctx,
		`SELECT id, workspace_id, person_id, sender_name, content, created_at
		 FROM chat_messages
		 WHERE workspace_id = $1 AND created_at < $2
		 ORDER BY created_at DESC
		 LIMIT $3`,
		workspaceID, before, limit,
	)
	if err != nil {
		return nil, fmt.Errorf("list messages: %w", err)
	}
	defer rows.Close()

	var messages []entities.ChatMessage
	for rows.Next() {
		var m entities.ChatMessage
		if err := rows.Scan(&m.ID, &m.WorkspaceID, &m.PersonID, &m.SenderName, &m.Content, &m.CreatedAt); err != nil {
			return nil, err
		}
		messages = append(messages, m)
	}
	if err := rows.Err(); err != nil {
		return nil, err
	}

	// Reverse DESC results to oldest-first order.
	for i, j := 0, len(messages)-1; i < j; i, j = i+1, j-1 {
		messages[i], messages[j] = messages[j], messages[i]
	}
	return messages, nil
}
