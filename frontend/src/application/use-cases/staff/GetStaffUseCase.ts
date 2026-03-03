import type { IStaffRepository } from '@/domain/repositories/IStaffRepository';

export class GetStaffUseCase {
  constructor(private readonly repo: IStaffRepository) {}
  execute() { return this.repo.getAll(); }
}
