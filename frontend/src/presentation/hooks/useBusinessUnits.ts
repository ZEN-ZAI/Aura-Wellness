import { useBusinessUnitStore } from '@/application/stores/businessUnitStore';

export function useBusinessUnits() {
  return useBusinessUnitStore();
}
