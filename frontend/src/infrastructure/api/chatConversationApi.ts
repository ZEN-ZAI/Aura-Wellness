import axiosClient from '../http/axiosClient';
import type { IChatConversationRepository } from '@/domain/repositories/IChatConversationRepository';
import type { ChatConversation } from '@/domain/entities/ChatConversation';
import type { ChatMessage } from '@/domain/entities/ChatMessage';

export const chatConversationApi: IChatConversationRepository = {
  listConversations: (buId: string) =>
    axiosClient
      .get<{ conversations: ChatConversation[] }>(`/chat/workspace/${buId}/conversations`)
      .then((r) => r.data.conversations ?? []),

  getOrCreateDM: (buId: string, targetPersonId: string) =>
    axiosClient
      .post<ChatConversation>(`/chat/workspace/${buId}/conversations/dm`, { targetPersonId })
      .then((r) => r.data),

  getMessages: (buId: string, conversationId: string, limit = 50, before?: string) =>
    axiosClient
      .get<{ messages: ChatMessage[] }>(
        `/chat/workspace/${buId}/conversations/${conversationId}/messages`,
        { params: { limit, ...(before ? { before } : {}) } },
      )
      .then((r) => r.data.messages ?? []),

  sendMessage: (buId: string, conversationId: string, content: string) =>
    axiosClient
      .post<ChatMessage>(
        `/chat/workspace/${buId}/conversations/${conversationId}/messages`,
        { content },
      )
      .then((r) => r.data),
};
