'use client';

import { useState } from 'react';
import { Form, Input, Button, Alert, Card, Typography, Result } from 'antd';
import Link from 'next/link';

const { Title, Text } = Typography;

interface OnboardFields {
  companyName: string;
  ownerFirstName: string;
  ownerEmail: string;
  ownerPassword: string;
  ownerConfirmPassword: string;
}

export default function OnboardPage() {
  const [form] = Form.useForm<OnboardFields>();
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);

  async function handleSubmit(values: OnboardFields) {
    setError('');
    setLoading(true);
    try {
      const { ownerConfirmPassword: _, ...payload } = values;
      const res = await fetch('/api/companies/onboard', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });
      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        setError((data as { error?: string }).error ?? 'Onboarding failed. Please try again.');
        return;
      }
      setSuccess(true);
    } catch {
      setError('Network error. Please try again.');
    } finally {
      setLoading(false);
    }
  }

  if (success) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <Card className="w-full max-w-md shadow-md text-center">
          <Result
            status="success"
            title="Company Registered!"
            subTitle="Your account is ready. Sign in with the email and password you provided."
            extra={
              <Link href="/login">
                <Button type="primary" block>
                  Go to Login
                </Button>
              </Link>
            }
          />
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12">
      <Card className="w-full max-w-lg shadow-md">
        <Title level={3} className="!mb-6">
          Register Your Company
        </Title>

        {error && (
          <Alert message={error} type="error" showIcon className="mb-4" />
        )}

        <Form form={form} layout="vertical" onFinish={handleSubmit}>
          <Text strong className="text-gray-500 uppercase text-xs tracking-wide block mb-3">
            Company Info
          </Text>

          <Form.Item
            label="Company Name"
            name="companyName"
            rules={[{ required: true }]}
          >
            <Input />
          </Form.Item>

          <Text strong className="text-gray-500 uppercase text-xs tracking-wide block mb-3 mt-4">
            Company Owner
          </Text>

          <Form.Item
            label="First Name"
            name="ownerFirstName"
            rules={[{ required: true }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            label="Email Address"
            name="ownerEmail"
            rules={[{ required: true, type: 'email' }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            label="Password"
            name="ownerPassword"
            rules={[
              { required: true, message: 'Password required' },
              { min: 8, message: 'Minimum 8 characters' },
            ]}
          >
            <Input.Password />
          </Form.Item>

          <Form.Item
            label="Confirm Password"
            name="ownerConfirmPassword"
            dependencies={['ownerPassword']}
            rules={[
              { required: true, message: 'Please confirm your password' },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue('ownerPassword') === value) {
                    return Promise.resolve();
                  }
                  return Promise.reject(new Error('Passwords do not match'));
                },
              }),
            ]}
          >
            <Input.Password />
          </Form.Item>

          <Form.Item className="!mb-2">
            <Button type="primary" htmlType="submit" loading={loading} block>
              Register Company
            </Button>
          </Form.Item>
        </Form>

        <Text className="text-center block text-gray-500 text-sm">
          Already have an account?{' '}
          <Link href="/login" className="text-indigo-600 hover:underline">
            Sign in
          </Link>
        </Text>
      </Card>
    </div>
  );
}
