'use client';

import { useEffect, useState } from 'react';
import { Button, Modal, Space, Typography, Alert } from 'antd';
import { PlusOutlined, UserAddOutlined } from '@ant-design/icons';
import { useStaff } from '@/presentation/hooks/useStaff';
import { StaffTable } from '@/presentation/components/staff/StaffTable';
import { StaffForm } from '@/presentation/components/staff/StaffForm';
import { EnrollExistingForm } from '@/presentation/components/staff/EnrollExistingForm';
import type { StaffMember } from '@/domain/entities/StaffMember';
import type { BusinessUnit } from '@/domain/entities/BusinessUnit';
import type { CreateStaffInput, EnrollExistingInput } from '@/domain/entities/StaffMember';

const { Title, Text } = Typography;

export default function StaffPageClient({
  initialStaff,
  initialBus,
  isOwner,
  defaultPassword,
}: {
  initialStaff: StaffMember[];
  initialBus: BusinessUnit[];
  isOwner: boolean;
  defaultPassword: string;
}) {
  const store = useStaff();
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showEnrollModal, setShowEnrollModal] = useState(false);
  const [createError, setCreateError] = useState('');
  const [enrollError, setEnrollError] = useState('');
  const [isCreating, setIsCreating] = useState(false);
  const [isEnrolling, setIsEnrolling] = useState(false);
  const [isUpdatingRole, setIsUpdatingRole] = useState<string | null>(null);
  const [passwordModalName, setPasswordModalName] = useState<string | null>(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    store.hydrate(initialStaff, initialBus);
    setReady(true);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function handleCreate(values: CreateStaffInput) {
    setCreateError('');
    setIsCreating(true);
    try {
      await store.create(values);
      setShowCreateModal(false);
      setPasswordModalName(`${values.firstName} ${values.lastName}`);
    } catch (err: unknown) {
      const e = err as { response?: { data?: { error?: string } } };
      setCreateError(e.response?.data?.error ?? 'Failed to create staff.');
    } finally {
      setIsCreating(false);
    }
  }

  async function handleEnroll(values: EnrollExistingInput) {
    setEnrollError('');
    setIsEnrolling(true);
    try {
      const person = store.persons.find((p) => p.personId === values.personId);
      await store.enrollExisting(values);
      setShowEnrollModal(false);
      if (person) setPasswordModalName(`${person.firstName} ${person.lastName}`);
    } catch (err: unknown) {
      const e = err as { response?: { data?: { error?: string } } };
      setEnrollError(e.response?.data?.error ?? 'Failed to enroll staff member.');
    } finally {
      setIsEnrolling(false);
    }
  }

  async function handleOpenEnroll() {
    await store.fetchPersons();
    setShowEnrollModal(true);
  }

  async function handleRoleChange(personId: string, role: string) {
    setIsUpdatingRole(personId);
    try {
      await store.updateRole(personId, role);
    } finally {
      setIsUpdatingRole(null);
    }
  }

  return (
    <div data-ready={ready || undefined}>
      <div className="flex items-center justify-between mb-6">
        <Title level={3} style={{ margin: 0 }}>
          Staff Management
        </Title>
        {isOwner && (
          <Space>
            <Button
              icon={<UserAddOutlined />}
              onClick={handleOpenEnroll}
            >
              Enroll in Another BU
            </Button>
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => setShowCreateModal(true)}
            >
              Add Staff Member
            </Button>
          </Space>
        )}
      </div>

      <StaffTable
        staff={ready ? store.staff : initialStaff}
        isLoading={store.isLoading}
        isOwner={isOwner}
        isUpdatingRole={isUpdatingRole}
        onRoleChange={handleRoleChange}
      />

      <StaffForm
        open={showCreateModal}
        businessUnits={ready ? store.businessUnits : initialBus}
        isCreating={isCreating}
        error={createError}
        onSubmit={handleCreate}
        onCancel={() => {
          setShowCreateModal(false);
          setCreateError('');
        }}
      />

      <EnrollExistingForm
        open={showEnrollModal}
        businessUnits={ready ? store.businessUnits : initialBus}
        persons={store.persons}
        isSubmitting={isEnrolling}
        error={enrollError}
        onSubmit={handleEnroll}
        onCancel={() => {
          setShowEnrollModal(false);
          setEnrollError('');
        }}
      />

      <Modal
        title="Staff Member Created"
        open={!!passwordModalName}
        onOk={() => setPasswordModalName(null)}
        onCancel={() => setPasswordModalName(null)}
        cancelButtonProps={{ style: { display: 'none' } }}
        okText="Got it"
      >
        <Alert
          type="info"
          showIcon
          className="mb-3"
          message={<><strong>{passwordModalName}</strong> has been added.</>}
        />
        <Text>Their temporary login password is:</Text>
        <div className="mt-2 p-3 bg-gray-100 rounded font-mono text-base text-center select-all">
          {defaultPassword}
        </div>
        <Text type="secondary" className="text-xs block mt-2">
          Ask them to change it after their first login.
        </Text>
      </Modal>
    </div>
  );
}
