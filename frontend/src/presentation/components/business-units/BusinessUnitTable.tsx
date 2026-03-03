'use client';

import { Table, Typography } from 'antd';
import type { BusinessUnit } from '@/domain/entities/BusinessUnit';

const { Text } = Typography;

interface BusinessUnitTableProps {
  businessUnits: BusinessUnit[];
  isLoading: boolean;
}

export function BusinessUnitTable({ businessUnits, isLoading }: BusinessUnitTableProps) {
  const columns = [
    { title: 'Name', dataIndex: 'name', key: 'name' },
    {
      title: 'Created',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (v: string) => new Date(v).toLocaleDateString(),
    },
  ];

  return (
    <Table
      dataSource={businessUnits}
      columns={columns}
      rowKey="id"
      pagination={false}
      loading={isLoading}
    />
  );
}
