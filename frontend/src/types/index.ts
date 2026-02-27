export interface AuthUser {
  token: string;
  personId: string;
  buId: string;
  companyId: string;
  role: 'Owner' | 'Admin' | 'Staff';
  firstName: string;
  lastName: string;
}

export interface BusinessUnit {
  id: string;
  companyId: string;
  name: string;
  createdAt: string;
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

export interface ChatWorkspace {
  workspaceId: string;
  buId: string;
  buName: string;
  members: ChatMember[];
}

export interface ChatMember {
  personId: string;
  firstName: string;
  lastName: string;
  role: string;
  hasAccess: boolean;
}

export interface BuChoice {
  buId: string;
  buName: string;
}
