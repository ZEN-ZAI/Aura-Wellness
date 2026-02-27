import client from './client';
import type { BuChoice } from '../types';

interface LoginResponse {
  requiresBuSelection?: boolean;
  choices?: BuChoice[];
  token?: string;
  personId?: string;
  buId?: string;
  companyId?: string;
  role?: string;
  firstName?: string;
  lastName?: string;
}

export async function login(email: string, password: string, buId?: string): Promise<LoginResponse> {
  const res = await client.post<LoginResponse>('/auth/login', { email, password, buId });
  return res.data;
}

export async function onboard(data: {
  companyName: string;
  address: string;
  contactNumber: string;
  ownerFirstName: string;
  ownerLastName: string;
  ownerEmail: string;
}) {
  const res = await client.post('/companies/onboard', data);
  return res.data;
}
