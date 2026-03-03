'use client';

import { Table, Tag, Select } from 'antd';
import type { StaffMember } from '@/domain/entities/StaffMember';

const roleColor: Record<string, string> = {
  Owner: 'purple',
  Admin: 'blue',
  Staff: 'default',
};

interface StaffTableProps {
  staff: StaffMember[];
  isLoading: boolean;
  isOwner: boolean;
  isUpdatingRole: string | null;
  onRoleChange: (personId: string, role: string) => void;
}

export function StaffTable({ staff, isLoading, isOwner, isUpdatingRole, onRoleChange }: StaffTableProps) {
  const columns = [
    {
      title: 'Name',
      key: 'name',
      render: (s: StaffMember) => `${s.firstName} ${s.lastName}`,
    },
    { title: 'Email', dataIndex: 'email', key: 'email' },
    { title: 'Business Unit', dataIndex: 'buName', key: 'buName' },
    {
      title: 'Role',
      key: 'role',
      render: (s: StaffMember) => (
        <Tag color={roleColor[s.role] ?? 'default'}>{s.role}</Tag>
      ),
    },
    ...(isOwner
      ? [
          {
            title: 'Actions',
            key: 'actions',
            render: (s: StaffMember) =>
              s.role !== 'Owner' ? (
                <Select
                  value={s.role}
                  size="small"
                  style={{ width: 110 }}
                  onChange={(role) => onRoleChange(s.personId, role)}
                  loading={isUpdatingRole === s.personId}
                  options={[
                    { label: 'Admin', value: 'Admin' },
                    { label: 'Staff', value: 'Staff' },
                  ]}
                />
              ) : null,
          },
        ]
      : []),
  ];

  return (
    <Table
      dataSource={staff}
      columns={columns}
      rowKey="profileId"
      pagination={false}
      loading={isLoading}
    />
  );
}
