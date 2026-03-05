export interface ChatConversation {
  conversationId: string;
  type: 'group' | 'dm';
  workspaceId: string;
  participants: ChatConversationParticipant[];
}

export interface ChatConversationParticipant {
  personId: string;
  firstName: string;
  lastName: string;
}
