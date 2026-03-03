import { cookies } from 'next/headers';
import type { AuthUser } from '@/domain/entities/AuthUser';

export async function getAuthUser(): Promise<AuthUser | null> {
  const cookieStore = await cookies();
  const raw = cookieStore.get('user_info')?.value;
  if (!raw) return null;
  try {
    return JSON.parse(raw) as AuthUser;
  } catch {
    return null;
  }
}
