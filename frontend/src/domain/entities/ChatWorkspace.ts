export interface ChatMember {
  personId: string;
  firstName: string;
  lastName: string;
  role: string;
  hasAccess: boolean;
}

export interface ChatWorkspace {
  workspaceId: string;
  buId: string;
  buName: string;
  members: ChatMember[];
}
