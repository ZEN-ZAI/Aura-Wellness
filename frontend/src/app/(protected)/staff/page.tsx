import { serverFetch } from '@/infrastructure/http/serverFetch';
import { getAuthUser } from '@/infrastructure/auth/cookieAuth';
import type { StaffMember } from '@/domain/entities/StaffMember';
import type { BusinessUnit } from '@/domain/entities/BusinessUnit';
import StaffPageClient from './StaffPageClient';

export default async function StaffPage() {
  const [user, staff, bus] = await Promise.all([
    getAuthUser(),
    serverFetch<StaffMember[]>('staff'),
    serverFetch<BusinessUnit[]>('business-units'),
  ]);

  const defaultPassword = process.env.DEFAULT_STAFF_PASSWORD ?? 'P@ssw0rd';

  return <StaffPageClient initialStaff={staff} initialBus={bus} isOwner={user?.role === 'Owner'} defaultPassword={defaultPassword} />;
}
