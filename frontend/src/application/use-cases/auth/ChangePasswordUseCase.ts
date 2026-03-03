import type { IAuthRepository } from '@/domain/repositories/IAuthRepository';

export class ChangePasswordUseCase {
  constructor(private readonly repo: IAuthRepository) {}
  execute(currentPassword: string, newPassword: string) {
    return this.repo.changePassword(currentPassword, newPassword);
  }
}
