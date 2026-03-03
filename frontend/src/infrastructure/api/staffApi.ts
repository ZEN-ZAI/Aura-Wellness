import axiosClient from '../http/axiosClient';
import type { IStaffRepository } from '@/domain/repositories/IStaffRepository';
import type { StaffMember, CreateStaffInput, EnrollExistingInput, PersonOption } from '@/domain/entities/StaffMember';

export const staffApi: IStaffRepository = {
  getAll: () =>
    axiosClient.get<StaffMember[]>('/staff').then((r) => r.data),

  getPersons: () =>
    axiosClient.get<PersonOption[]>('/staff/persons').then((r) => r.data),

  create: (data: CreateStaffInput) =>
    axiosClient.post<StaffMember>('/staff', data).then((r) => r.data),

  enrollExisting: (data: EnrollExistingInput) =>
    axiosClient.post<StaffMember>('/staff/enroll', data).then((r) => r.data),

  updateRole: (personId: string, buId: string, role: string) =>
    axiosClient.put(`/staff/${personId}/role`, { role, buId }).then(() => undefined),
};
