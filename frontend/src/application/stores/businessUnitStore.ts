import { create } from 'zustand';
import { container } from '@/lib/container';
import type { BusinessUnit } from '@/domain/entities/BusinessUnit';

interface BusinessUnitState {
  businessUnits: BusinessUnit[];
  isLoading: boolean;
  error: string | null;
  hydrate: (bus: BusinessUnit[]) => void;
  fetch: () => Promise<void>;
  create: (name: string) => Promise<void>;
  clearError: () => void;
}

export const useBusinessUnitStore = create<BusinessUnitState>()((set, get) => ({
  businessUnits: [],
  isLoading: false,
  error: null,

  hydrate: (bus) => set({ businessUnits: bus }),

  clearError: () => set({ error: null }),

  fetch: async () => {
    set({ isLoading: true, error: null });
    try {
      const bus = await container.businessUnits.getAll.execute();
      set({ businessUnits: bus, isLoading: false });
    } catch {
      set({ error: 'Failed to load business units.', isLoading: false });
    }
  },

  create: async (name: string) => {
    // throws on error — caller handles
    await container.businessUnits.create.execute(name);
    await get().fetch();
  },
}));
