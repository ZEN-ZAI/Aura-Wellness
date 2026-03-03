import type { ChatMessage } from '../entities/ChatMessage';

export interface IChatMessageRepository {
  getMessages(buId: string, limit?: number, before?: string): Promise<ChatMessage[]>;
  sendMessage(buId: string, content: string): Promise<ChatMessage>;
}
