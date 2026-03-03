'use client';

import { useLayoutEffect } from 'react';
import { useAuthStore } from '@/application/stores/authStore';
import type { AuthUser } from '@/domain/entities/AuthUser';

export function Providers({
  children,
  initialUser,
}: {
  children: React.ReactNode;
  initialUser: AuthUser | null;
}) {
  const setUser = useAuthStore((s) => s.setUser);

  useLayoutEffect(() => {
    // Only set a non-null user. useLayoutEffect fires children-before-parents, so
    // ProtectedLayoutClient.useLayoutEffect (child) sets the correct user first.
    // If we blindly call setUser(null) here (e.g. when RootLayout is cached from
    // before login), we overwrite the user that ProtectedLayoutClient just set.
    // Logout is handled by useAuthStore.logout() directly — not by this effect.
    if (initialUser) setUser(initialUser);
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  return <>{children}</>;
}
