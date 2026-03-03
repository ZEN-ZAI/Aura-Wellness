import { useStaffStore } from '@/application/stores/staffStore';

export function useStaff() {
  return useStaffStore();
}
