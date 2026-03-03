import type { IStaffRepository } from '@/domain/repositories/IStaffRepository';
import type { EnrollExistingInput } from '@/domain/entities/StaffMember';

export class EnrollExistingUseCase {
  constructor(private readonly repo: IStaffRepository) {}
  execute(data: EnrollExistingInput) { return this.repo.enrollExisting(data); }
}
