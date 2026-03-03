import type { IChatMessageRepository } from '@/domain/repositories/IChatMessageRepository';

export class GetMessagesUseCase {
  constructor(private readonly repo: IChatMessageRepository) {}
  execute(buId: string) { return this.repo.getMessages(buId); }
}
