'use client';

import { useState } from 'react';
import { Form, Input, Button, Alert, Card, Typography } from 'antd';
import { container } from '@/lib/container';

const { Title } = Typography;

interface ChangePasswordFields {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export default function ChangePasswordPage() {
  const [form] = Form.useForm<ChangePasswordFields>();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  async function handleSubmit(values: ChangePasswordFields) {
    if (values.newPassword !== values.confirmPassword) {
      setError('New passwords do not match.');
      return;
    }
    setError('');
    setLoading(true);
    try {
      await container.auth.changePassword.execute(values.currentPassword, values.newPassword);
      setSuccess(true);
      form.resetFields();
    } catch (err: unknown) {
      const e = err as { response?: { data?: { error?: string } } };
      setError(e.response?.data?.error ?? 'Failed to change password.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div>
      <Title level={3} style={{ marginBottom: 24 }}>
        Change Password
      </Title>

      <Card style={{ maxWidth: 480 }}>
        {success && (
          <Alert
            message="Password changed successfully."
            type="success"
            showIcon
            className="mb-4"
            closable
            onClose={() => setSuccess(false)}
          />
        )}
        {error && (
          <Alert message={error} type="error" showIcon className="mb-4" />
        )}

        <Form form={form} layout="vertical" onFinish={handleSubmit}>
          <Form.Item
            label="Current Password"
            name="currentPassword"
            rules={[{ required: true, message: 'Current password is required' }]}
          >
            <Input.Password autoComplete="current-password" />
          </Form.Item>

          <Form.Item
            label="New Password"
            name="newPassword"
            rules={[
              { required: true, message: 'New password is required' },
              { min: 6, message: 'Password must be at least 6 characters' },
            ]}
          >
            <Input.Password autoComplete="new-password" />
          </Form.Item>

          <Form.Item
            label="Confirm New Password"
            name="confirmPassword"
            rules={[{ required: true, message: 'Please confirm your new password' }]}
          >
            <Input.Password autoComplete="new-password" />
          </Form.Item>

          <Button type="primary" htmlType="submit" loading={loading}>
            Change Password
          </Button>
        </Form>
      </Card>
    </div>
  );
}
