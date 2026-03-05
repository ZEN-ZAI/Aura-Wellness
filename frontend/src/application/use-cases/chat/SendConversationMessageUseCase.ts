import type { IChatConversationRepository } from '@/domain/repositories/IChatConversationRepository';

export class SendConversationMessageUseCase {
  constructor(private readonly repo: IChatConversationRepository) {}
  execute(buId: string, conversationId: string, content: string) {
    return this.repo.sendMessage(buId, conversationId, content);
  }
}
