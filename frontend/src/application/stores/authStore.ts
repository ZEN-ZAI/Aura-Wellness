import { create } from 'zustand';
import type { AuthUser } from '@/domain/entities/AuthUser';

interface AuthState {
  user: AuthUser | null;
  isOwner: boolean;
  isOwnerOrAdmin: boolean;
  setUser: (user: AuthUser | null) => void;
  logout: () => Promise<void>;
}

export const useAuthStore = create<AuthState>()((set) => ({
  user: null,
  isOwner: false,
  isOwnerOrAdmin: false,

  setUser: (user) =>
    set({
      user,
      isOwner: user?.role === 'Owner' || false,
      isOwnerOrAdmin: user?.role === 'Owner' || user?.role === 'Admin' || false,
    }),

  logout: async () => {
    await fetch('/api/auth/logout', { method: 'POST' });
    set({ user: null, isOwner: false, isOwnerOrAdmin: false });
  },
}));
