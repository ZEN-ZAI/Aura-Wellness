import type { StaffMember, CreateStaffInput, EnrollExistingInput, PersonOption } from '../entities/StaffMember';

export interface IStaffRepository {
  getAll(): Promise<StaffMember[]>;
  getPersons(): Promise<PersonOption[]>;
  create(data: CreateStaffInput): Promise<StaffMember>;
  enrollExisting(data: EnrollExistingInput): Promise<StaffMember>;
  updateRole(personId: string, buId: string, role: string): Promise<void>;
}
