/**
 * Unit tests for the login API route handler.
 * File under test: src/app/api/auth/login/route.ts
 */

const mockFetch = jest.fn();
global.fetch = mockFetch;

let POST: (req: Request) => Promise<Response>;

beforeAll(async () => {
  const mod = await import('../../app/api/auth/login/route');
  POST = mod.POST as typeof POST;
});

beforeEach(() => {
  jest.clearAllMocks();
});

function makeLoginRequest(body: object): Request {
  return new Request('http://localhost/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(body),
    headers: { 'Content-Type': 'application/json' },
  });
}

// ── Successful login sets httpOnly cookies ─────────────────────────────────────

test('valid_login_sets_auth_token_and_user_info_cookies', async () => {
  mockFetch.mockResolvedValue(
    new Response(
      JSON.stringify({
        token: 'jwt-abc',
        personId: 'p1',
        buId: 'b1',
        companyId: 'c1',
        role: 'Owner',
        firstName: 'Alice',
        lastName: 'Smith',
      }),
      { status: 200 }
    )
  );

  const req = makeLoginRequest({ email: 'alice@acme.com', password: 'pass' });
  const res = await POST(req);

  expect(res.status).toBe(200);

  const body = await res.json();
  expect(body.user.firstName).toBe('Alice');
  // Token must not leak to the client
  expect(body.token).toBeUndefined();

  const setCookies = res.headers.getSetCookie?.() ?? [];
  const combined = setCookies.join('; ');
  expect(combined).toContain('auth_token=jwt-abc');
  expect(combined).toContain('user_info=');
  expect(combined).toContain('HttpOnly');
});

// ── Invalid credentials forwarded ─────────────────────────────────────────────

test('invalid_credentials_returns_401', async () => {
  mockFetch.mockResolvedValue(
    new Response(JSON.stringify({ error: 'Invalid credentials.' }), { status: 401 })
  );

  const req = makeLoginRequest({ email: 'x@x.com', password: 'wrong' });
  const res = await POST(req);

  expect(res.status).toBe(401);
  const setCookies = res.headers.getSetCookie?.() ?? [];
  expect(setCookies).toHaveLength(0); // no cookies set on failure
});

// ── Multi-BU selection response ───────────────────────────────────────────────

test('multi_bu_selection_backend_array_returns_200_with_choices', async () => {
  // Backend returns 200 with an array (legacy multi-BU)
  mockFetch.mockResolvedValue(
    new Response(
      JSON.stringify([
        { buId: 'bu-1', buName: 'HQ' },
        { buId: 'bu-2', buName: 'Sales' },
      ]),
      { status: 200 }
    )
  );

  const req = makeLoginRequest({ email: 'multi@acme.com', password: 'pass' });
  const res = await POST(req);

  expect(res.status).toBe(200);
  const body = await res.json();
  expect(body.requiresBuSelection).toBe(true);
  expect(body.choices).toHaveLength(2);
  expect(body.choices[0].buName).toBe('HQ');
});

// ── Backend unreachable ───────────────────────────────────────────────────────

test('backend_unreachable_returns_503', async () => {
  mockFetch.mockRejectedValue(new Error('ECONNREFUSED'));

  const req = makeLoginRequest({ email: 'x@x.com', password: 'pass' });
  const res = await POST(req);

  expect(res.status).toBe(503);
});
