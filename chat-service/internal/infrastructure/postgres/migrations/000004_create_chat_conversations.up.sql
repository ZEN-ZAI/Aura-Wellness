CREATE TABLE chat_conversations (
    id           UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID        NOT NULL REFERENCES chat_workspaces(id) ON DELETE CASCADE,
    type         VARCHAR(10) NOT NULL CHECK (type IN ('group', 'dm')),
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Only one group conversation per workspace
CREATE UNIQUE INDEX idx_conversations_group_unique ON chat_conversations(workspace_id) WHERE type = 'group';
CREATE INDEX idx_conversations_workspace_id ON chat_conversations(workspace_id);
