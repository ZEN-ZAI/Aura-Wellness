import axiosClient from '../http/axiosClient';
import type { IChatRepository } from '@/domain/repositories/IChatRepository';
import type { ChatWorkspace } from '@/domain/entities/ChatWorkspace';

export const chatApi: IChatRepository = {
  getWorkspace: (buId: string) =>
    axiosClient.get<ChatWorkspace>(`/chat/workspace/${buId}`).then((r) => r.data),

  updateAccess: (buId: string, personId: string, hasAccess: boolean) =>
    axiosClient
      .put(`/chat/workspace/${buId}/members/${personId}/access`, { hasAccess })
      .then(() => undefined),
};
