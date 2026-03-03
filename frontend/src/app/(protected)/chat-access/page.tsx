'use client';

import { useEffect, useState } from 'react';
import { Select, Table, Tag, Button, Typography, Alert, Card } from 'antd';
import { useChatStore } from '@/application/stores/chatStore';
import { useBusinessUnitStore } from '@/application/stores/businessUnitStore';
import { useAuthStore } from '@/application/stores/authStore';
import type { ChatMember } from '@/domain/entities/ChatWorkspace';

const { Title, Text } = Typography;

export default function ChatAccessPage() {
  const { isOwner } = useAuthStore();
  const chatStore = useChatStore();
  const buStore = useBusinessUnitStore();
  const [selectedBuId, setSelectedBuId] = useState('');
  const [isUpdating, setIsUpdating] = useState<string | null>(null);

  // Fetch BU list on mount if not already loaded
  useEffect(() => {
    if (buStore.businessUnits.length === 0) {
      buStore.fetch();
    }
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // Fetch workspace when BU selection changes
  useEffect(() => {
    if (selectedBuId) {
      chatStore.fetchWorkspace(selectedBuId);
    }
  }, [selectedBuId]); // eslint-disable-line react-hooks/exhaustive-deps

  const workspace = selectedBuId ? chatStore.workspaces[selectedBuId] : undefined;

  async function handleAccessToggle(personId: string, hasAccess: boolean) {
    setIsUpdating(personId);
    try {
      await chatStore.updateAccess(selectedBuId, personId, hasAccess);
    } finally {
      setIsUpdating(null);
    }
  }

  const columns = [
    {
      title: 'Member',
      key: 'name',
      render: (m: ChatMember) => `${m.firstName} ${m.lastName}`,
    },
    {
      title: 'Workspace Role',
      key: 'role',
      render: (m: ChatMember) => (
        <Tag color={m.role === 'Admin' ? 'purple' : 'default'}>{m.role}</Tag>
      ),
    },
    {
      title: 'Chat Access',
      key: 'access',
      render: (m: ChatMember) => (
        <Tag color={m.hasAccess ? 'green' : 'red'}>
          {m.hasAccess ? 'Granted' : 'Denied'}
        </Tag>
      ),
    },
    ...(isOwner
      ? [
          {
            title: 'Action',
            key: 'action',
            render: (m: ChatMember) =>
              m.role !== 'Admin' ? (
                <Button
                  size="small"
                  danger={m.hasAccess}
                  onClick={() => handleAccessToggle(m.personId, !m.hasAccess)}
                  loading={isUpdating === m.personId}
                >
                  {m.hasAccess ? 'Revoke' : 'Grant'} Access
                </Button>
              ) : null,
          },
        ]
      : []),
  ];

  return (
    <div>
      <Title level={3} style={{ marginBottom: 4 }}>
        Chat Access Control
      </Title>
      <Text className="text-gray-500 block mb-6">
        Manage which staff members can access chat for each Business Unit.
      </Text>

      <div className="mb-6">
        <Text strong className="block mb-2">
          Select Business Unit
        </Text>
        <Select
          value={selectedBuId || undefined}
          onChange={setSelectedBuId}
          placeholder="Choose a Business Unit..."
          loading={buStore.isLoading}
          style={{ minWidth: 256 }}
          options={buStore.businessUnits.map((b) => ({ label: b.name, value: b.id }))}
        />
      </div>

      {selectedBuId && (
        <>
          {chatStore.error && (
            <Alert message={chatStore.error} type="error" showIcon className="mb-4" />
          )}
          {workspace && (
            <Card
              title={`Chat Workspace: ${workspace.buName}`}
              extra={
                <Text className="text-gray-400 text-xs">
                  Workspace ID: {workspace.workspaceId}
                </Text>
              }
            >
              <Table
                dataSource={workspace.members}
                columns={columns}
                rowKey="personId"
                pagination={false}
                loading={chatStore.isLoading}
                locale={{ emptyText: 'No members in this workspace yet.' }}
              />
            </Card>
          )}
        </>
      )}
    </div>
  );
}
