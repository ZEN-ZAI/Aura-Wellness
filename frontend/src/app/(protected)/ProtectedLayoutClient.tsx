'use client';

import { useLayoutEffect } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { Layout, Menu, Button, Typography, Tag } from 'antd';
import {
  DashboardOutlined,
  TeamOutlined,
  BankOutlined,
  MessageOutlined,
  CommentOutlined,
  LockOutlined,
  LogoutOutlined,
} from '@ant-design/icons';
import { useAuthStore } from '@/application/stores/authStore';
import type { AuthUser } from '@/domain/entities/AuthUser';

const { Sider, Content } = Layout;
const { Title, Text } = Typography;

export default function ProtectedLayoutClient({
  children,
  user,
}: {
  children: React.ReactNode;
  user: AuthUser;
}) {
  const router = useRouter();
  const pathname = usePathname();
  const { logout, setUser } = useAuthStore();

  // Sync the server-supplied user into the Zustand store on every mount/user change.
  // Providers.useLayoutEffect only runs on initial app mount, so client-side
  // navigations after login (where Providers is already mounted) would leave the
  // store stale. ProtectedLayoutClient always receives a fresh user prop from its
  // server component parent, making it the reliable sync point.
  useLayoutEffect(() => {
    setUser(user);
  }, [user.personId]); // personId is stable for a given session

  const isOwner = user.role === 'Owner';
  const isOwnerOrAdmin = isOwner || user.role === 'Admin';

  async function handleLogout() {
    await logout();
    router.push('/login');
    router.refresh();
  }

  const menuItems = [
    { key: '/dashboard', icon: <DashboardOutlined />, label: 'Dashboard' },
    { key: '/business-units', icon: <BankOutlined />, label: 'Business Units' },
    ...(isOwnerOrAdmin
      ? [{ key: '/staff', icon: <TeamOutlined />, label: 'Staff Management' }]
      : []),
    { key: '/chat', icon: <CommentOutlined />, label: 'Chat' },
    ...(isOwner
      ? [{ key: '/chat-access', icon: <MessageOutlined />, label: 'Chat Access' }]
      : []),
    { key: '/change-password', icon: <LockOutlined />, label: 'Change Password' },
  ];

  const roleColor: Record<string, string> = {
    Owner: 'purple',
    Admin: 'blue',
    Staff: 'default',
  };

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider width={256} theme="dark" style={{ display: 'flex', flexDirection: 'column' }}>
        <div style={{ padding: '24px 20px 16px', borderBottom: '1px solid rgba(255,255,255,0.1)' }}>
          <Title level={4} style={{ color: '#fff', margin: 0 }}>
            Aura Wellness
          </Title>
          <Text style={{ color: 'rgba(255,255,255,0.65)', fontSize: 13, display: 'block', marginTop: 4 }}>
            {user.firstName} {user.lastName}
          </Text>
          <Tag color={roleColor[user.role] ?? 'default'} style={{ marginTop: 6 }}>
            {user.role}
          </Tag>
        </div>

        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[pathname]}
          items={menuItems}
          onClick={({ key }) => router.push(key)}
          style={{ flex: 1, borderRight: 0 }}
        />

        <div style={{ padding: '16px' }}>
          <Button
            icon={<LogoutOutlined />}
            onClick={handleLogout}
            block
            type="text"
            style={{ color: 'rgba(255,255,255,0.65)', textAlign: 'left' }}
          >
            Sign Out
          </Button>
        </div>
      </Sider>

      <Layout>
        <Content style={{ padding: '32px' }}>{children}</Content>
      </Layout>
    </Layout>
  );
}
