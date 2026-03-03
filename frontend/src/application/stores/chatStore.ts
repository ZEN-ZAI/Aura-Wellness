import { create } from 'zustand';
import { container } from '@/lib/container';
import type { ChatWorkspace } from '@/domain/entities/ChatWorkspace';
import type { ChatMessage } from '@/domain/entities/ChatMessage';

interface ChatState {
  workspaces: Record<string, ChatWorkspace>;
  messages: Record<string, ChatMessage[]>;
  isLoading: boolean;
  error: string | null;
  fetchWorkspace: (buId: string) => Promise<void>;
  updateAccess: (buId: string, personId: string, hasAccess: boolean) => Promise<void>;
  fetchMessages: (buId: string) => Promise<void>;
  appendMessage: (buId: string, msg: ChatMessage) => void;
}

export const useChatStore = create<ChatState>()((set) => ({
  workspaces: {},
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
    // Optimistic update
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

  appendMessage: (buId: string, msg: ChatMessage) => {
    set((s) => {
      const existing = s.messages[buId] ?? [];
      // De-duplicate by messageId in case the history fetch already included this message
      if (existing.some((m) => m.messageId === msg.messageId)) return s;
      return { messages: { ...s.messages, [buId]: [...existing, msg] } };
    });
  },

  fetchMessages: async (buId: string) => {
    // Throws on error — callers handle access-denied and other failures explicitly.
    const msgs = await container.chat.getMessages.execute(buId);
    set((s) => ({ messages: { ...s.messages, [buId]: msgs } }));
  },
}));
