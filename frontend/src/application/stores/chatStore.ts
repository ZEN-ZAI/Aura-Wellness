import { create } from 'zustand';
import { container } from '@/lib/container';
import type { ChatWorkspace } from '@/domain/entities/ChatWorkspace';
import type { ChatMessage } from '@/domain/entities/ChatMessage';
import type { ChatConversation } from '@/domain/entities/ChatConversation';

interface ChatState {
  workspaces: Record<string, ChatWorkspace>;
  /** Conversations per BU (keyed by buId). */
  conversations: Record<string, ChatConversation[]>;
  /** Currently selected conversation id. */
  activeConversationId: string | null;
  /** Messages keyed by conversationId. */
  messages: Record<string, ChatMessage[]>;
  isLoading: boolean;
  error: string | null;

  fetchWorkspace: (buId: string) => Promise<void>;
  updateAccess: (buId: string, personId: string, hasAccess: boolean) => Promise<void>;
  fetchConversations: (buId: string) => Promise<void>;
  setActiveConversation: (conversationId: string) => void;
  createDM: (buId: string, targetPersonId: string) => Promise<ChatConversation>;
  fetchConversationMessages: (buId: string, conversationId: string) => Promise<void>;
  appendMessage: (conversationId: string, msg: ChatMessage) => void;

  // Legacy — kept for backward compat (delegates to group conversation internally)
  fetchMessages: (buId: string) => Promise<void>;
}

export const useChatStore = create<ChatState>()((set, get) => ({
  workspaces: {},
  conversations: {},
  activeConversationId: null,
  messages: {},
  isLoading: false,
  error: null,

  fetchWorkspace: async (buId: string) => {
    set({ isLoading: true, error: null });
    try {
      const ws = await container.chat.getWorkspace.execute(buId);
      set((s) => ({ workspaces: { ...s.workspaces, [buId]: ws }, isLoading: false }));
    } catch {
      set({ error: 'Failed to load chat workspace.', isLoading: false });
    }
  },

  updateAccess: async (buId: string, personId: string, hasAccess: boolean) => {
    await container.chat.updateAccess.execute(buId, personId, hasAccess);
    set((s) => {
      const ws = s.workspaces[buId];
      if (!ws) return s;
      return {
        workspaces: {
          ...s.workspaces,
          [buId]: {
            ...ws,
            members: ws.members.map((m) =>
              m.personId === personId ? { ...m, hasAccess } : m
            ),
          },
        },
      };
    });
  },

  fetchConversations: async (buId: string) => {
    const convs = await container.chat.getConversations.execute(buId);
    set((s) => ({
      conversations: { ...s.conversations, [buId]: convs },
    }));
    // Auto-select the group conversation if nothing is active
    const { activeConversationId } = get();
    if (!activeConversationId && convs.length > 0) {
      const group = convs.find((c) => c.type === 'group');
      set({ activeConversationId: (group ?? convs[0]).conversationId });
    }
  },

  setActiveConversation: (conversationId: string) => {
    set({ activeConversationId: conversationId });
  },

  createDM: async (buId: string, targetPersonId: string) => {
    const conv = await container.chat.createDM.execute(buId, targetPersonId);
    // Add to conversation list if not already present
    set((s) => {
      const existing = s.conversations[buId] ?? [];
      if (existing.some((c) => c.conversationId === conv.conversationId)) {
        return { activeConversationId: conv.conversationId };
      }
      return {
        conversations: { ...s.conversations, [buId]: [...existing, conv] },
        activeConversationId: conv.conversationId,
      };
    });
    return conv;
  },

  fetchConversationMessages: async (buId: string, conversationId: string) => {
    const msgs = await container.chat.getConversationMessages.execute(buId, conversationId);
    set((s) => ({ messages: { ...s.messages, [conversationId]: msgs } }));
  },

  appendMessage: (conversationId: string, msg: ChatMessage) => {
    set((s) => {
      const existing = s.messages[conversationId] ?? [];
      if (existing.some((m) => m.messageId === msg.messageId)) return s;
      return { messages: { ...s.messages, [conversationId]: [...existing, msg] } };
    });
  },

  // Legacy: fetch messages for the group conversation of a BU
  fetchMessages: async (buId: string) => {
    const msgs = await container.chat.getMessages.execute(buId);
    // Group messages under buId key for backward compat
    set((s) => ({ messages: { ...s.messages, [buId]: msgs } }));
  },
}));
