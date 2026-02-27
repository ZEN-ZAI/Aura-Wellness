import client from './client';
import type { BusinessUnit } from '../types';

export async function getBusinessUnits(): Promise<BusinessUnit[]> {
  const res = await client.get<BusinessUnit[]>('/business-units');
  return res.data;
}

export async function createBusinessUnit(name: string): Promise<BusinessUnit> {
  const res = await client.post<BusinessUnit>('/business-units', { name });
  return res.data;
}
