import type { IBusinessUnitRepository } from '@/domain/repositories/IBusinessUnitRepository';

export class GetBusinessUnitsUseCase {
  constructor(private readonly repo: IBusinessUnitRepository) {}
  execute() { return this.repo.getAll(); }
}
