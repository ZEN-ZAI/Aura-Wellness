import type { IStaffRepository } from '@/domain/repositories/IStaffRepository';

export class GetPersonsUseCase {
  constructor(private readonly repo: IStaffRepository) {}
  execute() { return this.repo.getPersons(); }
}
