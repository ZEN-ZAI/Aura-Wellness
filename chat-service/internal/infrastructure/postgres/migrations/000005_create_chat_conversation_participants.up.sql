CREATE TABLE chat_conversation_participants (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    conversation_id UUID        NOT NULL REFERENCES chat_conversations(id) ON DELETE CASCADE,
    person_id       UUID        NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(conversation_id, person_id)
);

CREATE INDEX idx_participants_conversation_id ON chat_conversation_participants(conversation_id);
CREATE INDEX idx_participants_person_id ON chat_conversation_participants(person_id);
