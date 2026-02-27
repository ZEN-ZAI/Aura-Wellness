import client from './client';
import type { StaffMember } from '../types';

export async function getStaff(): Promise<StaffMember[]> {
  const res = await client.get<StaffMember[]>('/staff');
  return res.data;
}

export async function createStaff(data: {
  firstName: string;
  lastName: string;
  buId: string;
  email: string;
  role: string;
}): Promise<StaffMember> {
  const res = await client.post<StaffMember>('/staff', data);
  return res.data;
}

export async function updateRole(personId: string, role: string): Promise<void> {
  await client.put(`/staff/${personId}/role`, { role });
}
