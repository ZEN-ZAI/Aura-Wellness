'use client';

import { useLayoutEffect, useState } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { Layout, Menu, Button, Typography, Tag, Modal, App } from 'antd';
import {
  DashboardOutlined,
  TeamOutlined,
  BankOutlined,
  MessageOutlined,
  CommentOutlined,
  LockOutlined,
  LogoutOutlined,
  WarningOutlined,
} from '@ant-design/icons';
import axiosClient from '@/infrastructure/http/axiosClient';
import { useAuthStore } from '@/application/stores/authStore';
import type { AuthUser } from '@/domain/entities/AuthUser';

const { Sider, Content } = Layout;
const { Text } = Typography;

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
  const { message } = App.useApp();
  const [resetting, setResetting] = useState(false);
  const [resettingRedis, setResettingRedis] = useState(false);

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

  function handleResetDb() {
    Modal.confirm({
      title: 'Reset database to initial state?',
      icon: <WarningOutlined style={{ color: '#faad14' }} />,
      content:
        'This will permanently delete all companies, business units, staff members, chat messages, and other data, then restore the original demo seed. This action cannot be undone.',
      okText: 'Yes, reset',
      okButtonProps: { danger: true },
      cancelText: 'Cancel',
      onOk: async () => {
        setResetting(true);
        try {
          await axiosClient.post('/admin/reset-db');
          message.success('Database reset to initial state.');
          router.refresh();
        } catch {
          message.error('Failed to reset database. Please try again.');
        } finally {
          setResetting(false);
        }
      },
    });
  }

  function handleResetRedis() {
    Modal.confirm({
      title: 'Flush Redis cache?',
      icon: <WarningOutlined style={{ color: '#faad14' }} />,
      content:
        'This will flush all Redis databases, clearing cached data and pub/sub state. Active chat connections will not be terminated but in-flight messages may be lost.',
      okText: 'Yes, flush',
      okButtonProps: { danger: true },
      cancelText: 'Cancel',
      onOk: async () => {
        setResettingRedis(true);
        try {
          await axiosClient.post('/admin/reset-redis');
          message.success('Redis flushed successfully.');
        } catch {
          message.error('Failed to flush Redis. Please try again.');
        } finally {
          setResettingRedis(false);
        }
      },
    });
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

  const roleTagStyle: React.CSSProperties = {
    borderRadius: 3,
    fontSize: 10,
    fontWeight: 600,
    letterSpacing: '0.08em',
    textTransform: 'uppercase',
    border: '1px solid rgba(201,169,110,0.35)',
    background: 'rgba(201,169,110,0.08)',
    color: '#C9A96E',
    padding: '0 7px',
  };

  return (
    <Layout style={{ height: '100vh' }}>
      {/* ── Sidebar ─────────────────────────────────────────────── */}
      <Sider
        width={260}
        theme="dark"
        style={{
          background: '#1A1C1E',
          borderRight: 'none',
          display: 'flex',
          flexDirection: 'column',
          boxShadow: '2px 0 16px rgba(0,0,0,0.18)',
        }}
      >
        {/* Brand block */}
        <div
          style={{
            padding: '28px 24px 22px',
            borderBottom: '1px solid rgba(201,169,110,0.15)',
          }}
        >
          {/* Logo mark + name */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 14 }}>
            <div
              style={{
                width: 36,
                height: 36,
                borderRadius: '50%',
                background: 'linear-gradient(135deg, #C9A96E 0%, #A87E50 100%)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                flexShrink: 0,
                boxShadow: '0 2px 8px rgba(168,126,80,0.4)',
              }}
            >
              <span
                style={{
                  color: '#fff',
                  fontSize: 15,
                  fontWeight: 700,
                  fontFamily: 'var(--font-display)',
                  letterSpacing: '0.02em',
                }}
              >
                A
              </span>
            </div>
            <div>
              <div
                style={{
                  color: '#F8F5F0',
                  fontSize: 16,
                  fontWeight: 600,
                  fontFamily: 'var(--font-display)',
                  letterSpacing: '0.04em',
                  lineHeight: 1.2,
                }}
              >
                Aura Wellness
              </div>
              <div
                style={{
                  color: 'rgba(201,169,110,0.7)',
                  fontSize: 9,
                  letterSpacing: '0.14em',
                  textTransform: 'uppercase',
                  marginTop: 2,
                }}
              >
                Aesthetic &amp; Wellness Centre
              </div>
            </div>
          </div>

          {/* User info */}
          <Text
            style={{
              color: 'rgba(248,245,240,0.55)',
              fontSize: 13,
              display: 'block',
              marginBottom: 6,
            }}
          >
            {user.firstName} {user.lastName}
          </Text>
          <Tag style={roleTagStyle}>{user.role}</Tag>
        </div>

        {/* Navigation */}
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[pathname]}
          items={menuItems}
          onClick={({ key }) => router.push(key)}
          style={{
            flex: 1,
            borderRight: 0,
            background: 'transparent',
            padding: '8px 0',
          }}
        />

        {/* Sign-out */}
        <div style={{ padding: '16px 16px 24px' }}>
          {isOwner && (
            <Button
              danger
              loading={resetting}
              onClick={handleResetDb}
              block
              style={{ marginBottom: 8, fontSize: 13 }}
            >
              Reset DB to Init
            </Button>
          )}
          {isOwner && (
            <Button
              danger
              loading={resettingRedis}
              onClick={handleResetRedis}
              block
              style={{ marginBottom: 8, fontSize: 13 }}
            >
              Reset Redis
            </Button>
          )}
          <Button
            icon={<LogoutOutlined />}
            onClick={handleLogout}
            block
            type="text"
            style={{
              color: 'rgba(248,245,240,0.45)',
              textAlign: 'left',
              fontSize: 13,
              height: 38,
            }}
          >
            Sign Out
          </Button>
        </div>
      </Sider>

      {/* ── Main content ────────────────────────────────────────── */}
      <Layout style={{ background: '#F8F5F0' }}>
        <Content style={{ padding: '36px 40px', height: '100%', overflowY: 'auto' }}>
          {children}
        </Content>
      </Layout>
    </Layout>
  );
}
