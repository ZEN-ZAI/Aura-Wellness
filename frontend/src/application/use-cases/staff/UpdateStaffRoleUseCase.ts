import type { IStaffRepository } from '@/domain/repositories/IStaffRepository';

export class UpdateStaffRoleUseCase {
  constructor(private readonly repo: IStaffRepository) {}
  execute(personId: string, buId: string, role: string) { return this.repo.updateRole(personId, buId, role); }
}
