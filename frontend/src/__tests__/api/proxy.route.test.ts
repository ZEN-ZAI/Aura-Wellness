/**
 * Unit tests for the BFF proxy route handler.
 * File under test: src/app/api/proxy/[...path]/route.ts
 *
 * Strategy:
 * - Mock `next/headers` to control the auth_token cookie
 * - Mock global `fetch` to control backend responses
 * - Import the handler functions directly and invoke them
 */

const mockCookiesStore = {
  get: jest.fn(),
};

jest.mock('next/headers', () => ({
  cookies: jest.fn().mockResolvedValue(mockCookiesStore),
}));

// We need to mock fetch before importing the route
const mockFetch = jest.fn();
global.fetch = mockFetch;

// Dynamic import after mocks are set up
let GET: (req: Request, ctx: { params: Promise<{ path: string[] }> }) => Promise<Response>;
let POST: (req: Request, ctx: { params: Promise<{ path: string[] }> }) => Promise<Response>;

beforeAll(async () => {
  const mod = await import('../../app/api/proxy/[...path]/route');
  GET = mod.GET as typeof GET;
  POST = mod.POST as typeof POST;
});

function makeRequest(url: string, method = 'GET', body?: string): Request {
  return new Request(url, { method, body });
}

function makeCtx(path: string[]): { params: Promise<{ path: string[] }> } {
  return { params: Promise.resolve({ path }) };
}

beforeEach(() => {
  jest.clearAllMocks();
  mockCookiesStore.get.mockReturnValue(undefined); // no token by default
});

// ── No token ──────────────────────────────────────────────────────────────────

test('no_token_returns_401', async () => {
  mockCookiesStore.get.mockReturnValue(undefined);

  const req = makeRequest('http://localhost/api/proxy/staff');
  const res = await GET(req, makeCtx(['staff']));

  expect(res.status).toBe(401);
  const body = await res.json();
  expect(body.error).toMatch(/unauthorized/i);
});

// ── Backend 401 clears cookies ─────────────────────────────────────────────────

test('backend_401_clears_cookies', async () => {
  mockCookiesStore.get.mockReturnValue({ value: 'valid-token' });
  mockFetch.mockResolvedValue(new Response('{"error":"expired"}', { status: 401 }));

  const req = makeRequest('http://localhost/api/proxy/staff');
  const res = await GET(req, makeCtx(['staff']));

  expect(res.status).toBe(401);
  const body = await res.json();
  expect(body.error).toMatch(/session expired/i);
  // Cookies are cleared via Set-Cookie in the response
  const setCookie = res.headers.get('set-cookie') ?? '';
  expect(setCookie).toMatch(/auth_token/);
});

// ── Backend 204 returns empty body ─────────────────────────────────────────────

test('backend_204_returns_204_empty', async () => {
  mockCookiesStore.get.mockReturnValue({ value: 'token' });
  mockFetch.mockResolvedValue(new Response(null, { status: 204 }));

  const req = makeRequest('http://localhost/api/proxy/staff/1');
  const res = await GET(req, makeCtx(['staff', '1']));

  expect(res.status).toBe(204);
  const text = await res.text();
  expect(text).toBe('');
});

// ── fetch throws → 503 ────────────────────────────────────────────────────────

test('fetch_throws_returns_503', async () => {
  mockCookiesStore.get.mockReturnValue({ value: 'token' });
  mockFetch.mockRejectedValue(new Error('ECONNREFUSED'));

  const req = makeRequest('http://localhost/api/proxy/staff');
  const res = await GET(req, makeCtx(['staff']));

  expect(res.status).toBe(503);
  const body = await res.json();
  expect(body.error).toMatch(/backend unreachable/i);
});

// ── GET does not forward body ─────────────────────────────────────────────────

test('get_request_does_not_send_body', async () => {
  mockCookiesStore.get.mockReturnValue({ value: 'token' });
  mockFetch.mockResolvedValue(new Response('[]', { status: 200 }));

  const req = makeRequest('http://localhost/api/proxy/staff', 'GET');
  await GET(req, makeCtx(['staff']));

  const [, init] = mockFetch.mock.calls[0] as [string, RequestInit];
  expect(init.body).toBeUndefined();
});

// ── POST forwards body ────────────────────────────────────────────────────────

test('post_request_forwards_json_body', async () => {
  mockCookiesStore.get.mockReturnValue({ value: 'token' });
  mockFetch.mockResolvedValue(new Response('{"id":1}', { status: 200 }));

  const body = JSON.stringify({ name: 'Alice' });
  const req = makeRequest('http://localhost/api/proxy/staff', 'POST', body);
  await POST(req, makeCtx(['staff']));

  const [, init] = mockFetch.mock.calls[0] as [string, RequestInit];
  expect(init.body).toBe(body);
});

// ── Success response proxied ──────────────────────────────────────────────────

test('success_response_returns_backend_json_and_status', async () => {
  mockCookiesStore.get.mockReturnValue({ value: 'my-jwt' });
  mockFetch.mockResolvedValue(new Response('{"data":"ok"}', { status: 200 }));

  const req = makeRequest('http://localhost/api/proxy/staff');
  const res = await GET(req, makeCtx(['staff']));

  expect(res.status).toBe(200);
  const body = await res.json();
  expect(body.data).toBe('ok');
});

// ── Query string forwarded ────────────────────────────────────────────────────

test('query_string_forwarded_to_backend', async () => {
  mockCookiesStore.get.mockReturnValue({ value: 'token' });
  mockFetch.mockResolvedValue(new Response('[]', { status: 200 }));

  const req = makeRequest('http://localhost/api/proxy/staff?page=2&limit=10');
  await GET(req, makeCtx(['staff']));

  const [url] = mockFetch.mock.calls[0] as [string, RequestInit];
  expect(url).toContain('?page=2&limit=10');
});

// ── Auth header set ───────────────────────────────────────────────────────────

test('auth_header_set_from_cookie', async () => {
  mockCookiesStore.get.mockReturnValue({ value: 'my-secret-token' });
  mockFetch.mockResolvedValue(new Response('{}', { status: 200 }));

  const req = makeRequest('http://localhost/api/proxy/staff');
  await GET(req, makeCtx(['staff']));

  const [, init] = mockFetch.mock.calls[0] as [string, RequestInit];
  const authHeader = (init.headers as Record<string, string>)['Authorization'];
  expect(authHeader).toBe('Bearer my-secret-token');
});
