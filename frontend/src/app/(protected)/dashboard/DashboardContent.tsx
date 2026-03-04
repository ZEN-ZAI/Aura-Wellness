'use client';

import { Statistic, Card, Typography, List } from 'antd';
import type { BusinessUnit } from '@/domain/entities/BusinessUnit';
import type { StaffMember } from '@/domain/entities/StaffMember';
import type { AuthUser } from '@/domain/entities/AuthUser';

const { Title, Text } = Typography;

export default function DashboardContent({
  user,
  bus,
  staff,
}: {
  user: AuthUser | null;
  bus: BusinessUnit[];
  staff: StaffMember[];
}) {
  return (
    <div>
      <Title
        level={3}
        style={{
          margin: 0,
          marginBottom: 4,
          fontFamily: 'var(--font-display)',
          letterSpacing: '0.03em',
          color: '#1A1C1E',
          fontWeight: 600,
        }}
      >
        Dashboard
      </Title>
      <Text style={{ color: '#7A7068', display: 'block', marginBottom: 32, fontSize: 14 }}>
        Welcome back, {user?.firstName} {user?.lastName}
      </Text>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        {[
          { label: 'Business Units', value: bus.length },
          { label: 'Staff Members', value: staff.length },
          { label: 'Your Role', value: user?.role ?? '' },
        ].map((item) => (
          <Card
            key={item.label}
            variant="borderless"
            style={{
              borderRadius: 8,
              border: '1px solid #EDE8E0',
              boxShadow: '0 2px 12px rgba(44,32,16,0.06)',
              background: '#fff',
            }}
          >
            <Statistic
              title={
                <span
                  style={{
                    color: '#7A7068',
                    fontSize: 10,
                    letterSpacing: '0.14em',
                    textTransform: 'uppercase',
                    fontWeight: 600,
                  }}
                >
                  {item.label}
                </span>
              }
              value={item.value}
              valueStyle={{
                color: '#A87E50',
                fontSize: 30,
                fontFamily: 'var(--font-display)',
                fontWeight: 600,
              }}
            />
          </Card>
        ))}
      </div>

      <Card
        title={
          <span
            style={{
              fontFamily: 'var(--font-display)',
              letterSpacing: '0.03em',
              color: '#2D2D2D',
              fontWeight: 600,
              fontSize: 16,
            }}
          >
            Business Units
          </span>
        }
        variant="borderless"
        style={{
          borderRadius: 8,
          border: '1px solid #EDE8E0',
          boxShadow: '0 2px 12px rgba(44,32,16,0.06)',
        }}
      >
        {bus.length > 0 ? (
          <List
            dataSource={bus}
            renderItem={(bu) => (
                <List.Item
                  extra={
                    <Text style={{ color: '#B8AEA5', fontSize: 12 }}>
                      {new Date(bu.createdAt).toLocaleDateString()}
                    </Text>
                  }
                >
                  <Text strong style={{ color: '#2D2D2D' }}>{bu.name}</Text>
                </List.Item>
              )
            }
          />
        ) : (
          <Text style={{ color: '#B8AEA5', fontSize: 13 }}>No business units yet.</Text>
        )}
      </Card>
    </div>
  );
}
