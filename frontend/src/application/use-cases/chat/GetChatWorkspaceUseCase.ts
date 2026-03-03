import type { IChatRepository } from '@/domain/repositories/IChatRepository';

export class GetChatWorkspaceUseCase {
  constructor(private readonly repo: IChatRepository) {}
  execute(buId: string) { return this.repo.getWorkspace(buId); }
}
