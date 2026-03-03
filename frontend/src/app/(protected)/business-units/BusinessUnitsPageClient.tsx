'use client';

import { useEffect, useRef, useState } from 'react';
import { Button, Modal, Input, Typography, Alert } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import { useBusinessUnits } from '@/presentation/hooks/useBusinessUnits';
import { BusinessUnitTable } from '@/presentation/components/business-units/BusinessUnitTable';
import type { BusinessUnit } from '@/domain/entities/BusinessUnit';

const { Title } = Typography;

export default function BusinessUnitsPageClient({
  initialBus,
  isOwner,
}: {
  initialBus: BusinessUnit[];
  isOwner: boolean;
}) {
  const store = useBusinessUnits();
  const [showModal, setShowModal] = useState(false);
  const [name, setName] = useState('');
  const [createError, setCreateError] = useState('');
  const [isCreating, setIsCreating] = useState(false);
  const hydrated = useRef(false);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    setReady(true);
  }, []);

  if (!hydrated.current) {
    store.hydrate(initialBus);
    hydrated.current = true;
  }

  async function handleCreate() {
    if (!name.trim()) return;
    setCreateError('');
    setIsCreating(true);
    try {
      await store.create(name.trim());
      setShowModal(false);
      setName('');
    } catch (err: unknown) {
      const e = err as { response?: { data?: { error?: string } } };
      setCreateError(e.response?.data?.error ?? 'Failed to create business unit.');
    } finally {
      setIsCreating(false);
    }
  }

  return (
    <div data-ready={ready || undefined}>
      <div className="flex items-center justify-between mb-6">
        <Title level={3} style={{ margin: 0 }}>
          Business Units
        </Title>
        {isOwner && (
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => setShowModal(true)}
          >
            New Business Unit
          </Button>
        )}
      </div>

      <BusinessUnitTable
        businessUnits={store.businessUnits}
        isLoading={store.isLoading}
      />

      <Modal
        title="New Business Unit"
        open={showModal}
        onCancel={() => {
          setShowModal(false);
          setName('');
          setCreateError('');
        }}
        onOk={handleCreate}
        okText="Create"
        confirmLoading={isCreating}
        okButtonProps={{ disabled: !name.trim() }}
        destroyOnHidden
      >
        {createError && (
          <Alert message={createError} type="error" showIcon className="mb-3" />
        )}
        <Input
          placeholder="e.g., Marketing Team"
          value={name}
          onChange={(e) => setName(e.target.value)}
          onPressEnter={handleCreate}
        />
      </Modal>
    </div>
  );
}
