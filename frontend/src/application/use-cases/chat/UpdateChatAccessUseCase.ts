import type { IChatRepository } from '@/domain/repositories/IChatRepository';

export class UpdateChatAccessUseCase {
  constructor(private readonly repo: IChatRepository) {}
  execute(buId: string, personId: string, hasAccess: boolean) {
    return this.repo.updateAccess(buId, personId, hasAccess);
  }
}
