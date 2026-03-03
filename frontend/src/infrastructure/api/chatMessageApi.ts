import axiosClient from '../http/axiosClient';
import type { IChatMessageRepository } from '@/domain/repositories/IChatMessageRepository';
import type { ChatMessage } from '@/domain/entities/ChatMessage';

export const chatMessageApi: IChatMessageRepository = {
  getMessages: (buId: string, limit = 50, before?: string) =>
    axiosClient
      .get<{ messages: ChatMessage[] }>(`/chat/workspace/${buId}/messages`, {
        params: { limit, ...(before ? { before } : {}) },
      })
      .then((r) => r.data.messages ?? []),

  sendMessage: (buId: string, content: string) =>
    axiosClient
      .post<ChatMessage>(`/chat/workspace/${buId}/messages`, { content })
      .then((r) => r.data),
};
