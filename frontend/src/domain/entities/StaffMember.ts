export interface CreateStaffInput {
  firstName: string;
  lastName: string;
  buId: string;
  email: string;
  role: string;
}

export interface EnrollExistingInput {
  personId: string;
  buId: string;
  email: string;
  role: string;
}

export interface PersonOption {
  personId: string;
  firstName: string;
  lastName: string;
}

export interface StaffMember {
  personId: string;
  profileId: string;
  buId: string;
  buName: string;
  firstName: string;
  lastName: string;
  email: string;
  role: string;
  createdAt: string;
}
