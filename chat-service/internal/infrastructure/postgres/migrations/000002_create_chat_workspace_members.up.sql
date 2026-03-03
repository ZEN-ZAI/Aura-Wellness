CREATE TABLE IF NOT EXISTS chat_workspace_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID NOT NULL REFERENCES chat_workspaces(id) ON DELETE CASCADE,
    person_id UUID NOT NULL,
    role VARCHAR(20) NOT NULL CHECK (role IN ('Admin', 'Member')),
    has_access BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(workspace_id, person_id)
);

CREATE INDEX IF NOT EXISTS idx_members_workspace_id ON chat_workspace_members(workspace_id);
CREATE INDEX IF NOT EXISTS idx_members_person_id ON chat_workspace_members(person_id);
