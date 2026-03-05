import type { IChatConversationRepository } from '@/domain/repositories/IChatConversationRepository';

export class CreateDMUseCase {
  constructor(private readonly repo: IChatConversationRepository) {}
  execute(buId: string, targetPersonId: string) {
    return this.repo.getOrCreateDM(buId, targetPersonId);
  }
}
