'use client';

import { Avatar, List, Typography } from 'antd';
import { TeamOutlined, UserOutlined } from '@ant-design/icons';
import type { ChatConversation } from '@/domain/entities/ChatConversation';

const { Text } = Typography;

interface ConversationListProps {
  conversations: ChatConversation[];
  activeConversationId: string | null;
  currentPersonId: string;
  buName: string;
  onSelect: (conversationId: string) => void;
  onStartDM: (targetPersonId: string) => void;
  /** All workspace members for starting new DMs. */
  availableDMTargets: { personId: string; firstName: string; lastName: string }[];
}

export function ConversationList({
  conversations,
  activeConversationId,
  currentPersonId,
  buName,
  onSelect,
  onStartDM,
  availableDMTargets,
}: ConversationListProps) {
  // Separate group and DM conversations
  const groupConv = conversations.find((c) => c.type === 'group');
  const dmConvs = conversations.filter((c) => c.type === 'dm');

  // Existing DM person IDs (the "other" participant)
  const existingDMPersonIds = new Set(
    dmConvs.flatMap((c) =>
      c.participants
        .filter((p) => p.personId !== currentPersonId)
        .map((p) => p.personId),
    ),
  );

  // People who don't have a DM yet
  const newDMTargets = availableDMTargets.filter(
    (t) => t.personId !== currentPersonId && !existingDMPersonIds.has(t.personId),
  );

  return (
    <div style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <Text
        strong
        style={{
          display: 'block',
          padding: '12px 16px 8px',
          fontSize: 12,
          color: '#6b7280',
          textTransform: 'uppercase',
          letterSpacing: '0.05em',
        }}
      >
        Conversations
      </Text>

      <div style={{ flex: 1, overflowY: 'auto' }}>
        {/* Group conversation */}
        {groupConv && (
          <ConversationItem
            key={groupConv.conversationId}
            icon={<TeamOutlined style={{ fontSize: 14 }} />}
            label={buName || 'Team Chat'}
            isActive={activeConversationId === groupConv.conversationId}
            onClick={() => onSelect(groupConv.conversationId)}
          />
        )}

        {/* Existing DM conversations */}
        {dmConvs.length > 0 && (
          <Text
            style={{
              display: 'block',
              padding: '12px 16px 4px',
              fontSize: 11,
              color: '#9ca3af',
              textTransform: 'uppercase',
              letterSpacing: '0.05em',
            }}
          >
            Direct Messages
          </Text>
        )}
        {dmConvs.map((conv) => {
          const other = conv.participants.find((p) => p.personId !== currentPersonId);
          const label = other
            ? `${other.firstName} ${other.lastName}`.trim()
            : 'Direct Message';
          return (
            <ConversationItem
              key={conv.conversationId}
              icon={
                <Avatar
                  size={20}
                  style={{ background: '#6b7280', fontSize: 10, lineHeight: '20px' }}
                >
                  {label.charAt(0).toUpperCase()}
                </Avatar>
              }
              label={label}
              isActive={activeConversationId === conv.conversationId}
              onClick={() => onSelect(conv.conversationId)}
            />
          );
        })}

        {/* New DM targets */}
        {newDMTargets.length > 0 && (
          <Text
            style={{
              display: 'block',
              padding: '12px 16px 4px',
              fontSize: 11,
              color: '#9ca3af',
              textTransform: 'uppercase',
              letterSpacing: '0.05em',
            }}
          >
            Start a conversation
          </Text>
        )}
        {newDMTargets.map((target) => {
          const label = `${target.firstName} ${target.lastName}`.trim();
          return (
            <ConversationItem
              key={target.personId}
              icon={<UserOutlined style={{ fontSize: 14, color: '#9ca3af' }} />}
              label={label}
              isActive={false}
              onClick={() => onStartDM(target.personId)}
              muted
            />
          );
        })}
      </div>
    </div>
  );
}

function ConversationItem({
  icon,
  label,
  isActive,
  onClick,
  muted,
}: {
  icon: React.ReactNode;
  label: string;
  isActive: boolean;
  onClick: () => void;
  muted?: boolean;
}) {
  return (
    <div
      onClick={onClick}
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: 10,
        padding: '8px 16px',
        cursor: 'pointer',
        background: isActive ? '#eef2ff' : 'transparent',
        borderLeft: isActive ? '3px solid #4f46e5' : '3px solid transparent',
        opacity: muted ? 0.6 : 1,
      }}
      onMouseEnter={(e) => {
        if (!isActive) e.currentTarget.style.background = '#f9fafb';
      }}
      onMouseLeave={(e) => {
        if (!isActive) e.currentTarget.style.background = 'transparent';
      }}
    >
      {icon}
      <Text
        style={{
          fontSize: 13,
          fontWeight: isActive ? 600 : 400,
          overflow: 'hidden',
          textOverflow: 'ellipsis',
          whiteSpace: 'nowrap',
          color: muted ? '#9ca3af' : '#111827',
        }}
      >
        {label}
      </Text>
    </div>
  );
}
