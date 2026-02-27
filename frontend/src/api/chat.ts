import client from './client';
import type { ChatWorkspace } from '../types';

export async function getChatWorkspace(buId: string): Promise<ChatWorkspace> {
  const res = await client.get<ChatWorkspace>(`/chat/workspace/${buId}`);
  return res.data;
}

export async function updateChatAccess(buId: string, personId: string, hasAccess: boolean): Promise<void> {
  await client.put(`/chat/workspace/${buId}/members/${personId}/access`, { hasAccess });
}
