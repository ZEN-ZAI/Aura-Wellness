import { serverFetch } from '@/infrastructure/http/serverFetch';
import { getAuthUser } from '@/infrastructure/auth/cookieAuth';
import type { BusinessUnit } from '@/domain/entities/BusinessUnit';
import type { StaffMember } from '@/domain/entities/StaffMember';
import DashboardContent from './DashboardContent';

export default async function DashboardPage() {
  const user = await getAuthUser();
  const canViewStaff = user?.role === 'Owner' || user?.role === 'Admin';
  const [bus, staff] = await Promise.all([
    serverFetch<BusinessUnit[]>('business-units'),
    canViewStaff ? serverFetch<StaffMember[]>('staff') : Promise.resolve([] as StaffMember[]),
  ]);

  return <DashboardContent user={user} bus={bus} staff={staff} />;
}
