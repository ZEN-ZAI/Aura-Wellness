'use client';

import { Avatar, Badge, List, Tag, Typography } from 'antd';
import { LockOutlined } from '@ant-design/icons';
import type { ChatMember } from '@/domain/entities/ChatWorkspace';

const { Text } = Typography;

interface MemberListProps {
  members: ChatMember[];
  currentPersonId: string;
}

const roleColor: Record<string, string> = {
  Admin: 'purple',
  Owner: 'gold',
  Member: 'default',
};

export function MemberList({ members, currentPersonId }: MemberListProps) {
  return (
    <div style={{ height: '100%', overflowY: 'auto' }}>
      <Text
        strong
        style={{ display: 'block', padding: '12px 16px 8px', fontSize: 12, color: '#6b7280', textTransform: 'uppercase', letterSpacing: '0.05em' }}
      >
        Members · {members.length}
      </Text>
      <List
        dataSource={members}
        split={false}
        renderItem={(member) => {
          const isMe = member.personId === currentPersonId;
          const initials =
            `${member.firstName.charAt(0)}${member.lastName.charAt(0)}`.toUpperCase();

          return (
            <List.Item
              style={{
                padding: '6px 16px',
                opacity: member.hasAccess ? 1 : 0.45,
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: 10, width: '100%' }}>
                <Badge dot status={member.hasAccess ? 'success' : 'default'} offset={[-2, 28]}>
                  <Avatar
                    size={32}
                    style={{
                      background: isMe ? '#4f46e5' : '#6b7280',
                      fontSize: 12,
                      flexShrink: 0,
                    }}
                  >
                    {initials}
                  </Avatar>
                </Badge>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                    <Text
                      style={{
                        fontSize: 13,
                        fontWeight: isMe ? 600 : 400,
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        whiteSpace: 'nowrap',
                      }}
                    >
                      {member.firstName} {member.lastName}
                      {isMe && (
                        <Text style={{ fontSize: 11, color: '#9ca3af', marginLeft: 4 }}>
                          (you)
                        </Text>
                      )}
                    </Text>
                    {!member.hasAccess && (
                      <LockOutlined style={{ fontSize: 11, color: '#9ca3af' }} />
                    )}
                  </div>
                  <Tag
                    color={roleColor[member.role] ?? 'default'}
                    style={{ fontSize: 10, lineHeight: '16px', padding: '0 4px', marginTop: 1 }}
                  >
                    {member.role}
                  </Tag>
                </div>
              </div>
            </List.Item>
          );
        }}
      />
    </div>
  );
}
