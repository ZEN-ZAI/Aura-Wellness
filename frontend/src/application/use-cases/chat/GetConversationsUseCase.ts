import type { IChatConversationRepository } from '@/domain/repositories/IChatConversationRepository';

export class GetConversationsUseCase {
  constructor(private readonly repo: IChatConversationRepository) {}
  execute(buId: string) {
    return this.repo.listConversations(buId);
  }
}
