import type { IStaffRepository } from '@/domain/repositories/IStaffRepository';
import type { CreateStaffInput } from '@/domain/entities/StaffMember';

export class CreateStaffUseCase {
  constructor(private readonly repo: IStaffRepository) {}
  execute(data: CreateStaffInput) { return this.repo.create(data); }
}
