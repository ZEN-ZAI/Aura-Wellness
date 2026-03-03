import { serverFetch } from '@/infrastructure/http/serverFetch';
import { getAuthUser } from '@/infrastructure/auth/cookieAuth';
import type { BusinessUnit } from '@/domain/entities/BusinessUnit';
import BusinessUnitsPageClient from './BusinessUnitsPageClient';

export default async function BusinessUnitsPage() {
  const [user, bus] = await Promise.all([
    getAuthUser(),
    serverFetch<BusinessUnit[]>('business-units'),
  ]);
  return <BusinessUnitsPageClient initialBus={bus} isOwner={user?.role === 'Owner'} />;
}
