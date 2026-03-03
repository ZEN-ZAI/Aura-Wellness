import type { ChatWorkspace } from '../entities/ChatWorkspace';

export interface IChatRepository {
  getWorkspace(buId: string): Promise<ChatWorkspace>;
  updateAccess(buId: string, personId: string, hasAccess: boolean): Promise<void>;
}
