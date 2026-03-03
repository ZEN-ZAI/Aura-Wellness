export type UserRole = 'Owner' | 'Admin' | 'Staff';

export interface AuthUser {
  personId: string;
  buId: string;
  companyId: string;
  role: UserRole;
  firstName: string;
  lastName: string;
}
