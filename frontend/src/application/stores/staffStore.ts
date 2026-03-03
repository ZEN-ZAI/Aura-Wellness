import { create } from 'zustand';
import { container } from '@/lib/container';
import type { StaffMember, CreateStaffInput, EnrollExistingInput, PersonOption } from '@/domain/entities/StaffMember';
import type { BusinessUnit } from '@/domain/entities/BusinessUnit';

interface StaffState {
  staff: StaffMember[];
  businessUnits: BusinessUnit[];
  persons: PersonOption[];
  isLoading: boolean;
  error: string | null;
  hydrate: (staff: StaffMember[], bus: BusinessUnit[]) => void;
  fetch: () => Promise<void>;
  fetchPersons: () => Promise<void>;
  create: (data: CreateStaffInput) => Promise<void>;
  enrollExisting: (data: EnrollExistingInput) => Promise<void>;
  updateRole: (personId: string, role: string) => Promise<void>;
  clearError: () => void;
}

export const useStaffStore = create<StaffState>()((set, get) => ({
  staff: [],
  businessUnits: [],
  persons: [],
  isLoading: false,
  error: null,

  hydrate: (staff, bus) => set({ staff, businessUnits: bus }),

  clearError: () => set({ error: null }),

  fetch: async () => {
    set({ isLoading: true, error: null });
    try {
      const [staff, bus] = await Promise.all([
        container.staff.getAll.execute(),
        container.businessUnits.getAll.execute(),
      ]);
      set({ staff, businessUnits: bus, isLoading: false });
    } catch {
      set({ error: 'Failed to load staff data.', isLoading: false });
    }
  },

  fetchPersons: async () => {
    const persons = await container.staff.getPersons.execute();
    set({ persons });
  },

  create: async (data: CreateStaffInput) => {
    await container.staff.create.execute(data);
    await get().fetch();
  },

  enrollExisting: async (data: EnrollExistingInput) => {
    await container.staff.enrollExisting.execute(data);
    await get().fetch();
  },

  updateRole: async (personId: string, role: string) => {
    const member = get().staff.find((m) => m.personId === personId);
    if (!member) throw new Error('Staff member not found.');
    await container.staff.updateRole.execute(personId, member.buId, role);
    set((s) => ({
      staff: s.staff.map((m) => (m.personId === personId ? { ...m, role } : m)),
    }));
  },
}));
