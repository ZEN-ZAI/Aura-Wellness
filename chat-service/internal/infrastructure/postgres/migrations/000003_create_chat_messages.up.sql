CREATE TABLE chat_messages (
    id           UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID        NOT NULL REFERENCES chat_workspaces(id) ON DELETE CASCADE,
    person_id    UUID        NOT NULL,
    sender_name  TEXT        NOT NULL,
    content      TEXT        NOT NULL,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_messages_workspace_id_created_at ON chat_messages(workspace_id, created_at DESC);
