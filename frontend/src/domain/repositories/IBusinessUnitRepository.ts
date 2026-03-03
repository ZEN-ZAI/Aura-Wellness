import type { BusinessUnit } from '../entities/BusinessUnit';

export interface IBusinessUnitRepository {
  getAll(): Promise<BusinessUnit[]>;
  create(name: string): Promise<BusinessUnit>;
}
