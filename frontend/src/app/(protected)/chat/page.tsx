'use client';

import { useCallback, useEffect, useRef, useState } from 'react';
import { Layout, Skeleton, Result, Typography } from 'antd';
import { MessageOutlined, LockOutlined } from '@ant-design/icons';
import { useAuthStore } from '@/application/stores/authStore';
import { useChatStore } from '@/application/stores/chatStore';
import { useChatWebSocket } from '@/presentation/hooks/useChatWebSocket';
import { MessageList } from '@/presentation/components/chat/MessageList';
import { MessageInput } from '@/presentation/components/chat/MessageInput';
import { MemberList } from '@/presentation/components/chat/MemberList';

const { Content, Sider } = Layout;
const { Title } = Typography;

export default function ChatPage() {
  const user = useAuthStore((s) => s.user);
  const { workspaces, messages, isLoading, fetchWorkspace, fetchMessages, appendMessage } = useChatStore();

  const [accessDenied, setAccessDenied] = useState(false);
  const [initialLoading, setInitialLoading] = useState(true);
  const [draft, setDraft] = useState('');
  const [sending, setSending] = useState(false);
  const [sendError, setSendError] = useState('');

  const scrollRef = useRef<HTMLDivElement | null>(null);

  const buId = user?.buId ?? '';

  // Real-time WebSocket — stable regardless of access state; harmless if access is denied
  const { sendMessage, isConnected } = useChatWebSocket(buId);

  // ─── Initial load ───────────────────────────────────────────────────────────
  useEffect(() => {
    if (!buId) return;

    setInitialLoading(true);
    setAccessDenied(false);

    Promise.all([
      fetchWorkspace(buId),
      fetchMessages(buId).catch((err: unknown) => {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 403) {
          setAccessDenied(true);
        } else {
          // Non-403 errors are already reflected in chatStore.error; surface them there.
          throw err;
        }
      }),
    ]).finally(() => setInitialLoading(false));
  }, [buId]); // eslint-disable-line react-hooks/exhaustive-deps

  // ─── Auto-scroll to bottom when new messages arrive ─────────────────────────
  const buMessages = messages[buId] ?? [];
  useEffect(() => {
    scrollRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [buMessages.length]);

  // ─── Send handler ────────────────────────────────────────────────────────────
  const handleSend = useCallback(() => {
    const content = draft.trim();
    if (!content || sending) return;

    setSendError('');

    if (isConnected()) {
      sendMessage(content);
      setDraft('');
    } else {
      // WebSocket not yet open — fall back to REST send and append the returned message directly.
      setSending(true);
      import('@/lib/container')
        .then(({ container }) => container.chat.sendMessage.execute(buId, content))
        .then((msg) => {
          setDraft('');
          appendMessage(buId, msg);
        })
        .catch(() => setSendError('Failed to send message. Please try again.'))
        .finally(() => setSending(false));
    }
  }, [draft, sending, isConnected, sendMessage, buId]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        handleSend();
      }
    },
    [handleSend],
  );

  // ─── Derive workspace & layout state ─────────────────────────────────────────
  const workspace = workspaces[buId];

  if (!user) return null;

  // ─── Loading skeleton ────────────────────────────────────────────────────────
  if (initialLoading) {
    return (
      <div style={{ padding: 24 }}>
        <Skeleton active paragraph={{ rows: 6 }} />
      </div>
    );
  }

  // ─── Access denied ───────────────────────────────────────────────────────────
  if (accessDenied) {
    return (
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          height: 'calc(100vh - 64px)',
        }}
      >
        <Result
          icon={<LockOutlined style={{ color: '#9ca3af' }} />}
          title="Chat access restricted"
          subTitle="You don't have access to this workspace chat. Contact your team owner to request access."
        />
      </div>
    );
  }

  // ─── Main chat layout ────────────────────────────────────────────────────────
  return (
    <Layout
      style={{
        height: 'calc(100vh - 64px)',
        background: '#fff',
        borderRadius: 8,
        overflow: 'hidden',
        border: '1px solid #f0f0f0',
      }}
    >
      {/* ── Left: messages + input ── */}
      <Content
        style={{
          display: 'flex',
          flexDirection: 'column',
          padding: 16,
          minWidth: 0,
        }}
      >
        {/* Header */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 8,
            marginBottom: 12,
            paddingBottom: 12,
            borderBottom: '1px solid #f3f4f6',
          }}
        >
          <MessageOutlined style={{ fontSize: 18, color: '#4f46e5' }} />
          <Title level={5} style={{ margin: 0 }}>
            {workspace?.buName ?? 'Team Chat'}
          </Title>
        </div>

        {/* Messages */}
        <MessageList
          messages={buMessages}
          currentPersonId={user.personId}
          scrollRef={scrollRef}
        />

        {/* Input */}
        <MessageInput
          value={draft}
          onChange={setDraft}
          onSend={handleSend}
          onKeyDown={handleKeyDown}
          sending={sending}
          sendError={sendError}
        />
      </Content>

      {/* ── Right: member list ── */}
      <Sider
        width={220}
        style={{
          background: '#f9fafb',
          borderLeft: '1px solid #f0f0f0',
          overflowY: 'auto',
        }}
      >
        <MemberList
          members={workspace?.members ?? []}
          currentPersonId={user.personId}
        />
      </Sider>
    </Layout>
  );
}
