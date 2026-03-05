import type { ChatConversation } from '../entities/ChatConversation';
import type { ChatMessage } from '../entities/ChatMessage';

export interface IChatConversationRepository {
  listConversations(buId: string): Promise<ChatConversation[]>;
  getOrCreateDM(buId: string, targetPersonId: string): Promise<ChatConversation>;
  getMessages(buId: string, conversationId: string, limit?: number, before?: string): Promise<ChatMessage[]>;
  sendMessage(buId: string, conversationId: string, content: string): Promise<ChatMessage>;
}
