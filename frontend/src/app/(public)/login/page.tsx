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

  /* decorative gold divider */
  const GoldDivider = () => (
    <div style={{ display: 'flex', alignItems: 'center', margin: '20px 0' }}>
      <div style={{ flex: 1, height: 1, background: '#E8E0D5' }} />
      <span style={{ padding: '0 12px', color: '#C9A96E', fontSize: 14, lineHeight: 1 }}>✦</span>
      <div style={{ flex: 1, height: 1, background: '#E8E0D5' }} />
    </div>
  );

  return (
    <div
      style={{
        height: '100vh',
        overflow: 'hidden',
        background: 'linear-gradient(160deg, #F8F5F0 0%, #EDE8E0 55%, #F0EBE3 100%)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '24px',
      }}
    >
      <div style={{ width: '100%', maxWidth: 420 }}>
        {/* Brand header */}
        <div style={{ textAlign: 'center', marginBottom: 36 }}>
          <div
            style={{
              width: 60,
              height: 60,
              borderRadius: '50%',
              background: 'linear-gradient(135deg, #C9A96E 0%, #A87E50 100%)',
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              marginBottom: 16,
              boxShadow: '0 4px 20px rgba(168,126,80,0.28)',
            }}
          >
            <span
              style={{
                color: '#fff',
                fontSize: 26,
                fontWeight: 700,
                fontFamily: 'var(--font-display)',
              }}
            >
              A
            </span>
          </div>
          <Title
            level={3}
            style={{
              margin: 0,
              color: '#1A1C1E',
              fontFamily: 'var(--font-display)',
              letterSpacing: '0.05em',
              fontWeight: 600,
            }}
          >
            Aura Wellness
          </Title>
          <Text
            style={{
              color: '#A87E50',
              fontSize: 11,
              letterSpacing: '0.16em',
              textTransform: 'uppercase',
              display: 'block',
              marginTop: 5,
            }}
          >
            Aesthetic &amp; Wellness Centre
          </Text>
        </div>

        {/* Login card */}
        <Card
          variant="borderless"
          style={{
            borderRadius: 12,
            boxShadow: '0 8px 40px rgba(44,32,16,0.10)',
            border: '1px solid #EDE8E0',
          }}
        >
          <Title
            level={5}
            style={{
              margin: '0 0 4px',
              color: '#2D2D2D',
              fontFamily: 'var(--font-display)',
              letterSpacing: '0.02em',
            }}
          >
            Welcome back
          </Title>
          <Text style={{ color: '#7A7068', fontSize: 13 }}>
            Sign in to your workspace
          </Text>

          <GoldDivider />

          <div
            style={{
              background: '#FDFAF4',
              border: '1px solid #E8D9B8',
              borderRadius: 8,
              padding: '8px 12px',
              marginBottom: 16,
              fontSize: 12,
              color: '#7A6040',
              lineHeight: 1.6,
            }}
          >
            <span style={{ fontWeight: 600, color: '#A87E50' }}>Demo credentials</span>
            <br />
            Email: <code style={{ background: '#F0E8D8', padding: '0 4px', borderRadius: 3 }}>Welcome@example.com</code>
            &ensp;Password: <code style={{ background: '#F0E8D8', padding: '0 4px', borderRadius: 3 }}>P@ssw0rd</code>
          </div>

          {error && (
            <Alert message={error} type="error" showIcon className="mb-4" />
          )}

          <Form
            form={form}
            layout="vertical"
            onFinish={handleSubmit}
            initialValues={{ email: 'Welcome@example.com', password: 'P@ssw0rd' }}
          >
            <Form.Item
              label="Email Address"
              name="email"
              rules={[{ required: false, type: 'email' }]}
            >
              <Input
                disabled={!!buChoices}
                autoComplete="off"
                placeholder="you@example.com"
                style={{ background: '#FDFCFA' }}
              />
            </Form.Item>

            <Form.Item
              label="Password"
              name="password"
              rules={[{ required: true, message: 'Password required' }]}
            >
              <Input.Password
                autoComplete="off"
                placeholder="••••••••"
                style={{ background: '#FDFCFA' }}
              />
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

            <Form.Item style={{ marginBottom: 8, marginTop: 8 }}>
              <Button
                type="primary"
                htmlType="submit"
                loading={loading}
                block
                style={{ height: 42, fontSize: 14, letterSpacing: '0.04em', fontWeight: 500 }}
              >
                {buChoices ? 'Continue' : 'Sign In'}
              </Button>
            </Form.Item>
          </Form>
        </Card>

        <Text
          style={{
            textAlign: 'center',
            display: 'block',
            marginTop: 20,
            color: '#7A7068',
            fontSize: 13,
          }}
        >
          New clinic?{' '}
          <Link href="/onboard" style={{ color: '#A87E50', fontWeight: 500 }}>
            Register here
          </Link>
        </Text>
      </div>
    </div>
  );
}
