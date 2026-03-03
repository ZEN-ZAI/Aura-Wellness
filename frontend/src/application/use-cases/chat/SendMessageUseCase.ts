import type { IChatMessageRepository } from '@/domain/repositories/IChatMessageRepository';

export class SendMessageUseCase {
  constructor(private readonly repo: IChatMessageRepository) {}
  execute(buId: string, content: string) { return this.repo.sendMessage(buId, content); }
}
