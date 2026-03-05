import type { IChatConversationRepository } from '@/domain/repositories/IChatConversationRepository';

export class GetConversationMessagesUseCase {
  constructor(private readonly repo: IChatConversationRepository) {}
  execute(buId: string, conversationId: string) {
    return this.repo.getMessages(buId, conversationId);
  }
}
