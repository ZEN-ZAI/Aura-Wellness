'use client';

import { Modal, Form, Input, Select, Alert } from 'antd';
import type { BusinessUnit } from '@/domain/entities/BusinessUnit';
import type { EnrollExistingInput, PersonOption } from '@/domain/entities/StaffMember';

interface EnrollExistingFormProps {
  open: boolean;
  businessUnits: BusinessUnit[];
  persons: PersonOption[];
  isSubmitting: boolean;
  error: string;
  onSubmit: (values: EnrollExistingInput) => void;
  onCancel: () => void;
}

export function EnrollExistingForm({
  open,
  businessUnits,
  persons,
  isSubmitting,
  error,
  onSubmit,
  onCancel,
}: EnrollExistingFormProps) {
  const [form] = Form.useForm<EnrollExistingInput>();

  function handleCancel() {
    form.resetFields();
    onCancel();
  }

  return (
    <Modal
      title="Enroll Existing Person in Another BU"
      open={open}
      onCancel={handleCancel}
      onOk={() => form.submit()}
      okText="Enroll"
      confirmLoading={isSubmitting}
      destroyOnHidden
    >
      {error && <Alert message={error} type="error" showIcon className="mb-4" />}
      <Form
        form={form}
        layout="vertical"
        onFinish={onSubmit}
        initialValues={{ role: 'Staff' }}
      >
        <Form.Item label="Person" name="personId" rules={[{ required: true }]}>
          <Select
            showSearch
            placeholder="Select existing person..."
            filterOption={(input, opt) =>
              (opt?.label as string ?? '').toLowerCase().includes(input.toLowerCase())
            }
            options={persons.map((p) => ({
              value: p.personId,
              label: `${p.firstName} ${p.lastName}`,
            }))}
          />
        </Form.Item>

        <Form.Item label="Business Unit" name="buId" rules={[{ required: true }]}>
          <Select placeholder="Select BU to enroll into...">
            {businessUnits.map((b) => (
              <Select.Option key={b.id} value={b.id}>
                {b.name}
              </Select.Option>
            ))}
          </Select>
        </Form.Item>

        <Form.Item
          label="BU-scoped Email"
          name="email"
          rules={[{ required: true, type: 'email' }]}
        >
          <Input placeholder="Email address for this BU" />
        </Form.Item>

        <Form.Item label="Role" name="role">
          <Select
            options={[
              { label: 'Admin', value: 'Admin' },
              { label: 'Staff', value: 'Staff' },
            ]}
          />
        </Form.Item>
      </Form>
    </Modal>
  );
}
