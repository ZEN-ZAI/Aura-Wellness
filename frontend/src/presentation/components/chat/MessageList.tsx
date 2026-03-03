'use client';

import { RefObject } from 'react';
import { Avatar, Typography } from 'antd';
import type { ChatMessage } from '@/domain/entities/ChatMessage';

const { Text } = Typography;

interface MessageListProps {
  messages: ChatMessage[];
  currentPersonId: string;
  scrollRef: RefObject<HTMLDivElement | null>;
}

export function MessageList({ messages, currentPersonId, scrollRef }: MessageListProps) {
  return (
    <div
      style={{
        flex: 1,
        overflowY: 'auto',
        border: '1px solid #f0f0f0',
        borderRadius: 8,
        padding: '16px',
        marginBottom: 12,
        background: '#fafafa',
      }}
    >
      {messages.length === 0 ? (
        <div style={{ textAlign: 'center', paddingTop: 48 }}>
          <Text className="text-gray-400">No messages yet. Say hello!</Text>
        </div>
      ) : (
        messages.map((msg) => {
          const isMe = msg.personId === currentPersonId;
          return (
            <div
              key={msg.messageId}
              style={{
                display: 'flex',
                flexDirection: isMe ? 'row-reverse' : 'row',
                alignItems: 'flex-end',
                gap: 8,
                marginBottom: 12,
              }}
            >
              <Avatar
                size="small"
                style={{ background: isMe ? '#4f46e5' : '#6b7280', flexShrink: 0 }}
              >
                {msg.senderName.charAt(0).toUpperCase()}
              </Avatar>
              <div style={{ maxWidth: '70%' }}>
                {!isMe && (
                  <Text style={{ fontSize: 11, color: '#9ca3af', display: 'block', marginBottom: 2 }}>
                    {msg.senderName}
                  </Text>
                )}
                <div
                  style={{
                    background: isMe ? '#4f46e5' : '#ffffff',
                    color: isMe ? '#fff' : '#111827',
                    border: isMe ? 'none' : '1px solid #e5e7eb',
                    borderRadius: isMe ? '12px 12px 4px 12px' : '12px 12px 12px 4px',
                    padding: '8px 12px',
                    wordBreak: 'break-word',
                    fontSize: 14,
                  }}
                >
                  {msg.content}
                </div>
                <Text
                  style={{
                    fontSize: 10,
                    color: '#9ca3af',
                    display: 'block',
                    marginTop: 2,
                    textAlign: isMe ? 'right' : 'left',
                  }}
                >
                  {new Date(msg.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </Text>
              </div>
            </div>
          );
        })
      )}
      <div ref={scrollRef} />
    </div>
  );
}
