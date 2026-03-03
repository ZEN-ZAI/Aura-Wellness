import type { Metadata } from 'next';
import { AntdRegistry } from '@ant-design/nextjs-registry';
import { ConfigProvider, App as AntdApp } from 'antd';
import { getAuthUser } from '@/infrastructure/auth/cookieAuth';
import { Providers } from '@/presentation/providers';
import './globals.css';

export const metadata: Metadata = {
  title: 'Aura Wellness',
  description: 'Wellness management platform',
};

export default async function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const user = await getAuthUser();

  return (
    <html lang="en">
      <body>
        <AntdRegistry>
          <ConfigProvider
            theme={{
              token: {
                colorPrimary: '#4f46e5',
                borderRadius: 6,
              },
            }}
          >
            <AntdApp>
              <Providers initialUser={user}>{children}</Providers>
            </AntdApp>
          </ConfigProvider>
        </AntdRegistry>
      </body>
    </html>
  );
}
