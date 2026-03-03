import { useChatStore } from '@/application/stores/chatStore';

export function useChat() {
  return useChatStore();
}
