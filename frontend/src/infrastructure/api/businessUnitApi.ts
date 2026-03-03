import axiosClient from '../http/axiosClient';
import type { IBusinessUnitRepository } from '@/domain/repositories/IBusinessUnitRepository';
import type { BusinessUnit } from '@/domain/entities/BusinessUnit';

export const businessUnitApi: IBusinessUnitRepository = {
  getAll: () =>
    axiosClient.get<BusinessUnit[]>('/business-units').then((r) => r.data),

  create: (name: string) =>
    axiosClient.post<BusinessUnit>('/business-units', { name }).then((r) => r.data),
};
