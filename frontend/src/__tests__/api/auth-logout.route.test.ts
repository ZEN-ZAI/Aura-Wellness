/**
 * Unit tests for the logout API route handler.
 * File under test: src/app/api/auth/logout/route.ts
 */

let POST: () => Promise<Response>;

beforeAll(async () => {
  const mod = await import('../../app/api/auth/logout/route');
  POST = mod.POST as typeof POST;
});

// Reads all Set-Cookie values from a response across Node versions
function getSetCookies(res: Response): string[] {
  // Node 20+ / modern browsers expose getSetCookie()
  if (typeof (res.headers as unknown as { getSetCookie?: () => string[] }).getSetCookie === 'function') {
    return (res.headers as unknown as { getSetCookie: () => string[] }).getSetCookie();
  }
  // Fallback: single set-cookie string (may be comma-joined)
  const raw = res.headers.get('set-cookie') ?? '';
  return raw ? [raw] : [];
}

// ── Cookie clearing ───────────────────────────────────────────────────────────

test('logout_clears_auth_token_cookie', async () => {
  const res = await POST();
  const combined = getSetCookies(res).join(' | ');
  expect(combined).toMatch(/auth_token/);
});

test('logout_clears_user_info_cookie', async () => {
  const res = await POST();
  const combined = getSetCookies(res).join(' | ');
  expect(combined).toMatch(/user_info/);
});

test('logout_sets_max_age_zero_to_expire_cookies', async () => {
  const res = await POST();
  const cookies = getSetCookies(res);
  const authCookie = cookies.find(c => c.includes('auth_token'));
  const userInfoCookie = cookies.find(c => c.includes('user_info'));
  expect(authCookie).toMatch(/Max-Age=0/i);
  expect(userInfoCookie).toMatch(/Max-Age=0/i);
});

test('logout_returns_200_with_success', async () => {
  const res = await POST();
  expect(res.status).toBe(200);
  const body = await res.json();
  expect(body.success).toBe(true);
});
