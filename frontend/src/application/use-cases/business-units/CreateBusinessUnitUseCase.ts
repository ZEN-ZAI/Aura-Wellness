import type { IBusinessUnitRepository } from '@/domain/repositories/IBusinessUnitRepository';

export class CreateBusinessUnitUseCase {
  constructor(private readonly repo: IBusinessUnitRepository) {}
  execute(name: string) { return this.repo.create(name); }
}
