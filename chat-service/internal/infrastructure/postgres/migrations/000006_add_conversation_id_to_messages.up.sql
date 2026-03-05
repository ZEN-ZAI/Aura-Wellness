-- Add conversation_id column to chat_messages (nullable initially for backfill)
ALTER TABLE chat_messages ADD COLUMN conversation_id UUID REFERENCES chat_conversations(id) ON DELETE CASCADE;

-- Create a default group conversation for each existing workspace
INSERT INTO chat_conversations (id, workspace_id, type)
SELECT gen_random_uuid(), id, 'group' FROM chat_workspaces;

-- Back-fill existing messages with the group conversation of their workspace
UPDATE chat_messages m
SET conversation_id = c.id
FROM chat_conversations c
WHERE c.workspace_id = m.workspace_id AND c.type = 'group';

-- Make conversation_id NOT NULL after backfill
ALTER TABLE chat_messages ALTER COLUMN conversation_id SET NOT NULL;

-- Add index for querying messages by conversation
CREATE INDEX idx_messages_conversation_id_created_at ON chat_messages(conversation_id, created_at DESC);

-- Add group conversation participants from existing workspace members
INSERT INTO chat_conversation_participants (id, conversation_id, person_id)
SELECT gen_random_uuid(), c.id, m.person_id
FROM chat_conversations c
JOIN chat_workspace_members m ON m.workspace_id = c.workspace_id
WHERE c.type = 'group';
