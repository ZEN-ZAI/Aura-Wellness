import { redirect } from 'next/navigation';
import { getAuthUser } from '@/infrastructure/auth/cookieAuth';
import ProtectedLayoutClient from './ProtectedLayoutClient';

export default async function ProtectedLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const user = await getAuthUser();
  if (!user) redirect('/login');

  return <ProtectedLayoutClient user={user}>{children}</ProtectedLayoutClient>;
}
