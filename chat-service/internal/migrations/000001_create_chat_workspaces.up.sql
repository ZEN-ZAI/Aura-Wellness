CREATE TABLE IF NOT EXISTS chat_workspaces (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bu_id UUID NOT NULL UNIQUE,
    company_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_chat_workspaces_company_id ON chat_workspaces(company_id);
