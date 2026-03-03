'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { Form, Input, Button, Select, Alert, Card, Typography } from 'antd';
import Link from 'next/link';
import { useAuthStore } from '@/application/stores/authStore';
import type { AuthUser } from '@/domain/entities/AuthUser';
import type { BuChoice } from '@/domain/entities/BuChoice';

const { Title, Text } = Typography;

interface LoginFields {
  email: string;
  password: string;
  buId?: string;
}

export default function LoginPage() {
  const router = useRouter();
  const setUser = useAuthStore((s) => s.setUser);
  const [form] = Form.useForm<LoginFields>();
  const [buChoices, setBuChoices] = useState<BuChoice[] | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  async function handleSubmit(values: LoginFields) {
    setError('');
    setLoading(true);
    try {
      const res = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          email: values.email,
          password: values.password,
          buId: values.buId,
        }),
      });

      const data = await res.json();

      if (data.requiresBuSelection) {
        setBuChoices(data.choices as BuChoice[]);
        form.setFieldValue('buId', data.choices[0]?.buId);
        setLoading(false);
        return;
      }

      if (!res.ok) {
        setError(data.error ?? 'Invalid credentials.');
        setLoading(false);
        return;
      }

      setUser(data.user as AuthUser);
      router.push('/dashboard');
      router.refresh();
    } catch {
      setError('Network error. Please try again.');
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <Card className="w-full max-w-sm shadow-md">
        <Title level={3} className="!mb-6">
          Sign In
        </Title>

        {error && (
          <Alert message={error} type="error" showIcon className="mb-4" />
        )}

        <Form
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
          initialValues={{ email: '', password: '' }}
        >
          <Form.Item
            label="Email Address"
            name="email"
            rules={[{ required: false, type: 'email'}]}
          >
            <Input disabled={!!buChoices} autoComplete="off" />
          </Form.Item>

          <Form.Item
            label="Password"
            name="password"
            rules={[{ required: true, message: 'Password required' }]}
          >
            <Input.Password autoComplete="off" />
          </Form.Item>

          {buChoices && (
            <Form.Item
              label="Select Business Unit"
              name="buId"
              rules={[{ required: true, message: 'Please select a business unit' }]}
            >
              <Select>
                {buChoices.map((c) => (
                  <Select.Option key={c.buId} value={c.buId}>
                    {c.buName}
                  </Select.Option>
                ))}
              </Select>
            </Form.Item>
          )}

          <Form.Item className="!mb-2">
            <Button type="primary" htmlType="submit" loading={loading} block>
              {buChoices ? 'Continue' : 'Sign In'}
            </Button>
          </Form.Item>
        </Form>

        <Text className="text-center block text-gray-500 text-sm">
          New company?{' '}
          <Link href="/onboard" className="text-indigo-600 hover:underline">
            Register here
          </Link>
        </Text>
      </Card>
    </div>
  );
}
