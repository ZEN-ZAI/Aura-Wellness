'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { Statistic, Card, Typography, List, Button, Modal, App } from 'antd';
import { WarningOutlined } from '@ant-design/icons';
import axiosClient from '@/infrastructure/http/axiosClient';
import type { BusinessUnit } from '@/domain/entities/BusinessUnit';
import type { StaffMember } from '@/domain/entities/StaffMember';
import type { AuthUser } from '@/domain/entities/AuthUser';

const { Title, Text } = Typography;

export default function DashboardContent({
  user,
  bus,
  staff,
}: {
  user: AuthUser | null;
  bus: BusinessUnit[];
  staff: StaffMember[];
}) {
  const router = useRouter();
  const { message } = App.useApp();
  const [resetting, setResetting] = useState(false);

  function handleResetDb() {
    Modal.confirm({
      title: 'Reset database to initial state?',
      icon: <WarningOutlined style={{ color: '#faad14' }} />,
      content:
        'This will permanently delete all companies, business units, staff members, and other data, then restore the original demo seed. This action cannot be undone.',
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

  return (
    <div>
      <div className="flex items-center justify-between mb-1">
        <Title level={3} style={{ margin: 0 }}>
          Dashboard
        </Title>
        {user?.role === 'Owner' && (
          <Button
            danger
            loading={resetting}
            onClick={handleResetDb}
          >
            Reset DB to Init
          </Button>
        )}
      </div>
      <Text className="text-gray-500 block mb-8">
        Welcome back, {user?.firstName} {user?.lastName}
      </Text>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        <Card>
          <Statistic title="Business Units" value={bus.length} valueStyle={{ color: '#4f46e5' }} />
        </Card>
        <Card>
          <Statistic title="Staff Members" value={staff.length} valueStyle={{ color: '#16a34a' }} />
        </Card>
        <Card>
          <Statistic title="Your Role" value={user?.role ?? ''} valueStyle={{ color: '#7c3aed' }} />
        </Card>
      </div>

      <Card title="Business Units">
        {bus.length > 0 ? (
          <List
            dataSource={bus}
            renderItem={(bu) => (
              <List.Item
                extra={
                  <Text className="text-gray-400 text-xs">
                    {new Date(bu.createdAt).toLocaleDateString()}
                  </Text>
                }
              >
                <Text strong>{bu.name}</Text>
              </List.Item>
            )}
          />
        ) : (
          <Text className="text-gray-400 text-sm">No business units yet.</Text>
        )}
      </Card>
    </div>
  );
}
