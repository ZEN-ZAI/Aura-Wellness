'use client';

import { Modal, Form, Input, Select, Space, Button, Alert } from 'antd';
import type { BusinessUnit } from '@/domain/entities/BusinessUnit';
import type { CreateStaffInput } from '@/domain/entities/StaffMember';

interface StaffFormProps {
  open: boolean;
  businessUnits: BusinessUnit[];
  isCreating: boolean;
  error: string;
  onSubmit: (values: CreateStaffInput) => void;
  onCancel: () => void;
}

export function StaffForm({ open, businessUnits, isCreating, error, onSubmit, onCancel }: StaffFormProps) {
  const [form] = Form.useForm<CreateStaffInput>();

  function handleCancel() {
    form.resetFields();
    onCancel();
  }

  function handleFinish(values: CreateStaffInput) {
    onSubmit(values);
  }

  return (
    <Modal
      title="Add Staff Member"
      open={open}
      onCancel={handleCancel}
      footer={null}
      destroyOnHidden
    >
      {error && <Alert message={error} type="error" showIcon className="mb-4" />}
      <Form
        form={form}
        layout="vertical"
        onFinish={handleFinish}
        initialValues={{ role: 'Staff' }}
      >
        <Space.Compact block>
          <Form.Item
            label="First Name"
            name="firstName"
            rules={[{ required: true }]}
            style={{ flex: 1, marginRight: 8 }}
          >
            <Input />
          </Form.Item>
          <Form.Item
            label="Last Name"
            name="lastName"
            rules={[{ required: true }]}
            style={{ flex: 1 }}
          >
            <Input />
          </Form.Item>
        </Space.Compact>

        <Form.Item
          label="Email"
          name="email"
          rules={[{ required: true, type: 'email' }]}
        >
          <Input />
        </Form.Item>

        <Form.Item label="Business Unit" name="buId" rules={[{ required: true }]}>
          <Select placeholder="Select BU...">
            {businessUnits.map((b) => (
              <Select.Option key={b.id} value={b.id}>
                {b.name}
              </Select.Option>
            ))}
          </Select>
        </Form.Item>

        <Form.Item label="Role" name="role">
          <Select
            options={[
              { label: 'Admin', value: 'Admin' },
              { label: 'Staff', value: 'Staff' },
            ]}
          />
        </Form.Item>

        <div className="flex gap-3 justify-end">
          <Button onClick={handleCancel}>Cancel</Button>
          <Button type="primary" htmlType="submit" loading={isCreating}>
            Add Staff
          </Button>
        </div>
      </Form>
    </Modal>
  );
}
