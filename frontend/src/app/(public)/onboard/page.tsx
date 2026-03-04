'use client';

import React, { useState } from 'react';
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

  const pageWrapStyle: React.CSSProperties = {
    height: '100vh',
    overflow: 'hidden',
    background: 'linear-gradient(160deg, #F8F5F0 0%, #EDE8E0 55%, #F0EBE3 100%)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    padding: '40px 24px',
  };

  const sectionLabelStyle: React.CSSProperties = {
    display: 'block',
    marginBottom: 12,
    color: '#A87E50',
    fontSize: 10,
    fontWeight: 600,
    letterSpacing: '0.18em',
    textTransform: 'uppercase',
  };

  if (success) {
    return (
      <div style={pageWrapStyle}>
        <Card
          variant="borderless"
          style={{
            width: '100%',
            maxWidth: 480,
            borderRadius: 12,
            boxShadow: '0 8px 40px rgba(44,32,16,0.10)',
            border: '1px solid #EDE8E0',
            textAlign: 'center',
          }}
        >
          <Result
            status="success"
            title={
              <span style={{ fontFamily: 'var(--font-display)', letterSpacing: '0.03em' }}>
                Clinic Registered
              </span>
            }
            subTitle="Your workspace is ready. Sign in with the credentials you provided."
            extra={
              <Link href="/login">
                <Button
                  type="primary"
                  block
                  style={{ height: 42, letterSpacing: '0.04em', fontWeight: 500 }}
                >
                  Go to Sign In
                </Button>
              </Link>
            }
          />
        </Card>
      </div>
    );
  }

  return (
    <div style={pageWrapStyle}>
      <div style={{ width: '100%', maxWidth: 520 }}>
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
              margin: '0 0 2px',
              color: '#2D2D2D',
              fontFamily: 'var(--font-display)',
              letterSpacing: '0.02em',
            }}
          >
            Register Your Clinic
          </Title>
          <Text style={{ color: '#7A7068', fontSize: 13, display: 'block', marginBottom: 24 }}>
            Create your workspace to get started
          </Text>

          {error && (
            <Alert message={error} type="error" showIcon className="mb-4" />
          )}

          <Form form={form} layout="vertical" onFinish={handleSubmit}>
            <Text style={sectionLabelStyle}>Clinic Information</Text>

            <Form.Item
              label="Clinic / Company Name"
              name="companyName"
              rules={[{ required: true }]}
            >
              <Input
                placeholder="e.g. Lumière Aesthetic Clinic"
                style={{ background: '#FDFCFA' }}
              />
            </Form.Item>

            <Text style={{ ...sectionLabelStyle, marginTop: 8 }}>Owner Account</Text>

            <Form.Item
              label="First Name"
              name="ownerFirstName"
              rules={[{ required: true }]}
            >
              <Input placeholder="Your first name" style={{ background: '#FDFCFA' }} />
            </Form.Item>

            <Form.Item
              label="Email Address"
              name="ownerEmail"
              rules={[{ required: true, type: 'email' }]}
            >
              <Input placeholder="owner@clinic.com" style={{ background: '#FDFCFA' }} />
            </Form.Item>

            <Form.Item
              label="Password"
              name="ownerPassword"
              rules={[
                { required: true, message: 'Password required' },
                { min: 8, message: 'Minimum 8 characters' },
              ]}
            >
              <Input.Password
                placeholder="Minimum 8 characters"
                style={{ background: '#FDFCFA' }}
              />
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
              <Input.Password placeholder="Repeat password" style={{ background: '#FDFCFA' }} />
            </Form.Item>

            <Form.Item style={{ marginBottom: 8, marginTop: 8 }}>
              <Button
                type="primary"
                htmlType="submit"
                loading={loading}
                block
                style={{ height: 42, fontSize: 14, letterSpacing: '0.04em', fontWeight: 500 }}
              >
                Register Clinic
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
          Already have an account?{' '}
          <Link href="/login" style={{ color: '#A87E50', fontWeight: 500 }}>
            Sign in
          </Link>
        </Text>
      </div>
    </div>
  );
}
