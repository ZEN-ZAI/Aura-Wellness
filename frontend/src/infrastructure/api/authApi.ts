import axiosClient from '../http/axiosClient';
import type { IAuthRepository } from '@/domain/repositories/IAuthRepository';

export const authApi: IAuthRepository = {
  changePassword: (currentPassword: string, newPassword: string) =>
    axiosClient
      .put('/auth/password', { currentPassword, newPassword })
      .then(() => undefined),
};
