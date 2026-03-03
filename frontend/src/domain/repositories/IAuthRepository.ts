export interface IAuthRepository {
  changePassword(currentPassword: string, newPassword: string): Promise<void>;
}
