package postgres

import (
	"context"
	"fmt"

	"github.com/aura-wellness/chat-service/internal/domain/entities"
	"github.com/google/uuid"
	"github.com/jackc/pgx/v5/pgxpool"
)

type conversationRepo struct {
	pool *pgxpool.Pool
}

func NewConversationRepo(pool *pgxpool.Pool) *conversationRepo {
	return &conversationRepo{pool: pool}
}

func (r *conversationRepo) Create(ctx context.Context, c entities.ChatConversation) (entities.ChatConversation, error) {
	row := r.pool.QueryRow(ctx,
		`INSERT INTO chat_conversations (workspace_id, type)
		 VALUES ($1, $2)
		 RETURNING id, workspace_id, type, created_at`,
		c.WorkspaceID, c.Type,
	)
	var out entities.ChatConversation
	if err := row.Scan(&out.ID, &out.WorkspaceID, &out.Type, &out.CreatedAt); err != nil {
		return entities.ChatConversation{}, fmt.Errorf("create conversation: %w", err)
	}
	return out, nil
}

func (r *conversationRepo) GetByID(ctx context.Context, id uuid.UUID) (entities.ChatConversation, error) {
	row := r.pool.QueryRow(ctx,
		`SELECT id, workspace_id, type, created_at FROM chat_conversations WHERE id = $1`, id,
	)
	var out entities.ChatConversation
	if err := row.Scan(&out.ID, &out.WorkspaceID, &out.Type, &out.CreatedAt); err != nil {
		return entities.ChatConversation{}, fmt.Errorf("get conversation by id: %w", err)
	}
	return out, nil
}

func (r *conversationRepo) GetGroupByWorkspace(ctx context.Context, workspaceID uuid.UUID) (entities.ChatConversation, error) {
	row := r.pool.QueryRow(ctx,
		`SELECT id, workspace_id, type, created_at FROM chat_conversations
		 WHERE workspace_id = $1 AND type = 'group'`, workspaceID,
	)
	var out entities.ChatConversation
	if err := row.Scan(&out.ID, &out.WorkspaceID, &out.Type, &out.CreatedAt); err != nil {
		return entities.ChatConversation{}, fmt.Errorf("get group conversation: %w", err)
	}
	return out, nil
}

func (r *conversationRepo) GetDMByParticipants(ctx context.Context, workspaceID, personA, personB uuid.UUID) (entities.ChatConversation, error) {
	row := r.pool.QueryRow(ctx,
		`SELECT c.id, c.workspace_id, c.type, c.created_at
		 FROM chat_conversations c
		 WHERE c.workspace_id = $1 AND c.type = 'dm'
		   AND EXISTS (SELECT 1 FROM chat_conversation_participants p WHERE p.conversation_id = c.id AND p.person_id = $2)
		   AND EXISTS (SELECT 1 FROM chat_conversation_participants p WHERE p.conversation_id = c.id AND p.person_id = $3)`,
		workspaceID, personA, personB,
	)
	var out entities.ChatConversation
	if err := row.Scan(&out.ID, &out.WorkspaceID, &out.Type, &out.CreatedAt); err != nil {
		return entities.ChatConversation{}, fmt.Errorf("get dm conversation: %w", err)
	}
	return out, nil
}

func (r *conversationRepo) ListByWorkspaceAndPerson(ctx context.Context, workspaceID, personID uuid.UUID) ([]entities.ChatConversation, error) {
	rows, err := r.pool.Query(ctx,
		`SELECT c.id, c.workspace_id, c.type, c.created_at
		 FROM chat_conversations c
		 LEFT JOIN chat_conversation_participants p ON p.conversation_id = c.id
		 WHERE c.workspace_id = $1
		   AND (c.type = 'group' OR p.person_id = $2)
		 ORDER BY c.type ASC, c.created_at ASC`,
		workspaceID, personID,
	)
	if err != nil {
		return nil, fmt.Errorf("list conversations: %w", err)
	}
	defer rows.Close()

	var conversations []entities.ChatConversation
	for rows.Next() {
		var c entities.ChatConversation
		if err := rows.Scan(&c.ID, &c.WorkspaceID, &c.Type, &c.CreatedAt); err != nil {
			return nil, err
		}
		conversations = append(conversations, c)
	}
	return conversations, rows.Err()
}

func (r *conversationRepo) AddParticipant(ctx context.Context, p entities.ChatConversationParticipant) error {
	_, err := r.pool.Exec(ctx,
		`INSERT INTO chat_conversation_participants (conversation_id, person_id)
		 VALUES ($1, $2)
		 ON CONFLICT (conversation_id, person_id) DO NOTHING`,
		p.ConversationID, p.PersonID,
	)
	if err != nil {
		return fmt.Errorf("add participant: %w", err)
	}
	return nil
}

func (r *conversationRepo) ListParticipants(ctx context.Context, conversationID uuid.UUID) ([]entities.ChatConversationParticipant, error) {
	rows, err := r.pool.Query(ctx,
		`SELECT id, conversation_id, person_id, created_at
		 FROM chat_conversation_participants
		 WHERE conversation_id = $1
		 ORDER BY created_at ASC`,
		conversationID,
	)
	if err != nil {
		return nil, fmt.Errorf("list participants: %w", err)
	}
	defer rows.Close()

	var participants []entities.ChatConversationParticipant
	for rows.Next() {
		var p entities.ChatConversationParticipant
		if err := rows.Scan(&p.ID, &p.ConversationID, &p.PersonID, &p.CreatedAt); err != nil {
			return nil, err
		}
		participants = append(participants, p)
	}
	return participants, rows.Err()
}
