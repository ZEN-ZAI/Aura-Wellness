import type { Metadata } from 'next';
import { Playfair_Display, DM_Sans } from 'next/font/google';
import { AntdRegistry } from '@ant-design/nextjs-registry';
import { ConfigProvider, App as AntdApp } from 'antd';
import { getAuthUser } from '@/infrastructure/auth/cookieAuth';
import { Providers } from '@/presentation/providers';
import './globals.css';

const playfair = Playfair_Display({
  subsets: ['latin'],
  variable: '--font-display',
  weight: ['400', '500', '600', '700'],
  display: 'swap',
});

const dmSans = DM_Sans({
  subsets: ['latin'],
  variable: '--font-body',
  weight: ['300', '400', '500', '600'],
  display: 'swap',
});

export const metadata: Metadata = {
  title: 'Aura Wellness',
  description: 'Aesthetic & Wellness Centre',
};

export default async function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const user = await getAuthUser();

  return (
    <html lang="en" className={`${playfair.variable} ${dmSans.variable}`}>
      <body>
        <AntdRegistry>
          <ConfigProvider
            theme={{
              token: {
                /* ── Brand ──────────────────────────────── */
                colorPrimary:          '#A87E50',
                colorPrimaryHover:     '#C9A96E',
                colorPrimaryActive:    '#8A6438',
                /* ── Backgrounds ────────────────────────── */
                colorBgLayout:         '#F8F5F0',
                colorBgContainer:      '#FFFFFF',
                colorBgElevated:       '#FFFFFF',
                /* ── Text ───────────────────────────────── */
                colorText:             '#2D2D2D',
                colorTextSecondary:    '#7A7068',
                colorTextDisabled:     '#B8AEA5',
                /* ── Border ─────────────────────────────── */
                colorBorder:           '#E8E0D5',
                colorBorderSecondary:  '#F0EBE3',
                /* ── Geometry ───────────────────────────── */
                borderRadius:          4,
                borderRadiusLG:        8,
                borderRadiusSM:        3,
                /* ── Typography ─────────────────────────── */
                fontFamily:            'var(--font-body), DM Sans, Inter, system-ui, sans-serif',
                fontSize:              14,
                /* ── Shadow ─────────────────────────────── */
                boxShadow:             '0 2px 12px rgba(44, 32, 16, 0.08)',
                boxShadowSecondary:    '0 1px 4px rgba(44, 32, 16, 0.06)',
              },
              components: {
                Layout: {
                  siderBg:    '#1A1C1E',
                  triggerBg:  '#1A1C1E',
                },
                Menu: {
                  darkItemBg:          '#1A1C1E',
                  darkSubMenuItemBg:   '#141618',
                  darkItemSelectedBg:  'rgba(168, 126, 80, 0.18)',
                  darkItemHoverBg:     'rgba(201, 169, 110, 0.10)',
                  darkItemSelectedColor: '#C9A96E',
                  darkItemColor:       'rgba(248, 245, 240, 0.65)',
                  darkItemHoverColor:  '#F8F5F0',
                  itemBorderRadius:    4,
                },
                Button: {
                  borderRadius:        4,
                  controlHeight:       38,
                  paddingInline:       20,
                  fontWeight:          500,
                },
                Input: {
                  controlHeight:       38,
                  borderRadius:        4,
                  colorBgContainer:    '#FDFCFA',
                },
                Card: {
                  borderRadius:        8,
                  colorBorderSecondary: '#EDE8E0',
                },
                Table: {
                  colorBgContainer:    '#FFFFFF',
                  headerBg:            '#F8F5F0',
                  headerColor:         '#7A7068',
                },
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
