'use client';

import { useCallback, useEffect, useRef, useState } from 'react';
import { Layout, Skeleton, Result, Typography } from 'antd';
import { LockOutlined, TeamOutlined, UserOutlined } from '@ant-design/icons';
import { useAuthStore } from '@/application/stores/authStore';
import { useChatStore } from '@/application/stores/chatStore';
import { useChatWebSocket } from '@/presentation/hooks/useChatWebSocket';
import { MessageList } from '@/presentation/components/chat/MessageList';
import { MessageInput } from '@/presentation/components/chat/MessageInput';
import { MemberList } from '@/presentation/components/chat/MemberList';
import { ConversationList } from '@/presentation/components/chat/ConversationList';

const { Content, Sider } = Layout;
const { Title } = Typography;

export default function ChatPage() {
  const user = useAuthStore((s) => s.user);
  const {
    workspaces,
    conversations,
    activeConversationId,
    messages,
    fetchWorkspace,
    fetchConversations,
    fetchConversationMessages,
    setActiveConversation,
    createDM,
    appendMessage,
  } = useChatStore();

  const [accessDenied, setAccessDenied] = useState(false);
  const [initialLoading, setInitialLoading] = useState(true);
  const [draft, setDraft] = useState('');
  const [sending, setSending] = useState(false);
  const [sendError, setSendError] = useState('');

  const scrollRef = useRef<HTMLDivElement | null>(null);

  const buId = user?.buId ?? '';

  // Real-time WebSocket
  const { sendMessage, isConnected } = useChatWebSocket(buId);

  // ─── Initial load ───────────────────────────────────────────────────────────
  useEffect(() => {
    if (!buId) return;

    setInitialLoading(true);
    setAccessDenied(false);

    Promise.all([
      fetchWorkspace(buId),
      fetchConversations(buId).catch((err: unknown) => {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 403) {
          setAccessDenied(true);
        } else {
          throw err;
        }
      }),
    ]).finally(() => setInitialLoading(false));
  }, [buId]); // eslint-disable-line react-hooks/exhaustive-deps

  // ─── Fetch messages when active conversation changes ────────────────────────
  useEffect(() => {
    if (!buId || !activeConversationId) return;
    fetchConversationMessages(buId, activeConversationId).catch(() => {
      // Silently handled
    });
  }, [buId, activeConversationId]); // eslint-disable-line react-hooks/exhaustive-deps

  // ─── Auto-scroll to bottom when new messages arrive ─────────────────────────
  const convMessages = activeConversationId ? (messages[activeConversationId] ?? []) : [];
  useEffect(() => {
    scrollRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [convMessages.length]);

  // ─── Send handler ────────────────────────────────────────────────────────────
  const handleSend = useCallback(() => {
    const content = draft.trim();
    if (!content || sending || !activeConversationId) return;

    setSendError('');

    if (isConnected()) {
      sendMessage(activeConversationId, content);
      setDraft('');
    } else {
      // WebSocket not yet open — fall back to REST
      setSending(true);
      import('@/lib/container')
        .then(({ container }) =>
          container.chat.sendConversationMessage.execute(buId, activeConversationId, content),
        )
        .then((msg) => {
          setDraft('');
          appendMessage(activeConversationId, msg);
        })
        .catch(() => setSendError('Failed to send message. Please try again.'))
        .finally(() => setSending(false));
    }
  }, [draft, sending, isConnected, sendMessage, buId, activeConversationId, appendMessage]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        handleSend();
      }
    },
    [handleSend],
  );

  // ─── Conversation selection ─────────────────────────────────────────────────
  const handleSelectConversation = useCallback(
    (conversationId: string) => {
      setActiveConversation(conversationId);
      setDraft('');
      setSendError('');
    },
    [setActiveConversation],
  );

  const handleStartDM = useCallback(
    async (targetPersonId: string) => {
      if (!buId) return;
      await createDM(buId, targetPersonId);
      setDraft('');
      setSendError('');
    },
    [buId, createDM],
  );

  // ─── Derive workspace & layout state ─────────────────────────────────────────
  const workspace = workspaces[buId];
  const buConversations = conversations[buId] ?? [];
  const activeConv = buConversations.find(
    (c) => c.conversationId === activeConversationId,
  );

  // Build header label
  let headerLabel = workspace?.buName ?? 'Team Chat';
  let headerIcon = <TeamOutlined style={{ fontSize: 18, color: '#4f46e5' }} />;
  if (activeConv?.type === 'dm') {
    const other = activeConv.participants.find((p) => p.personId !== user?.personId);
    headerLabel = other
      ? `${other.firstName} ${other.lastName}`.trim()
      : 'Direct Message';
    headerIcon = <UserOutlined style={{ fontSize: 18, color: '#4f46e5' }} />;
  }

  // DM targets: workspace members with access
  const availableDMTargets = (workspace?.members ?? [])
    .filter((m) => m.hasAccess)
    .map((m) => ({
      personId: m.personId,
      firstName: m.firstName,
      lastName: m.lastName,
    }));

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
      {/* ── Left: conversation list ── */}
      <Sider
        width={220}
        style={{
          background: '#fff',
          borderRight: '1px solid #f0f0f0',
          overflowY: 'auto',
        }}
      >
        <ConversationList
          conversations={buConversations}
          activeConversationId={activeConversationId}
          currentPersonId={user.personId}
          buName={workspace?.buName ?? 'Team Chat'}
          onSelect={handleSelectConversation}
          onStartDM={handleStartDM}
          availableDMTargets={availableDMTargets}
        />
      </Sider>

      {/* ── Center: messages + input ── */}
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
          {headerIcon}
          <Title level={5} style={{ margin: 0 }}>
            {headerLabel}
          </Title>
        </div>

        {/* Messages */}
        <MessageList
          messages={convMessages}
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

      {/* ── Right: member list (group only) ── */}
      {activeConv?.type !== 'dm' && (
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
      )}
    </Layout>
  );
}
