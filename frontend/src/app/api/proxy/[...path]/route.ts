import { NextRequest, NextResponse } from 'next/server';
import { cookies } from 'next/headers';

const BACKEND_URL = process.env.BACKEND_URL ?? 'http://localhost:5239';

async function proxyRequest(
  req: NextRequest,
  params: { path: string[] }
): Promise<NextResponse> {
  const cookieStore = await cookies();
  const token = cookieStore.get('auth_token')?.value;

  if (!token) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const path = params.path.join('/');
  const url = new URL(req.url);
  const backendUrl = `${BACKEND_URL}/api/${path}${url.search}`;

  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${token}`,
  };

  const fetchOptions: RequestInit = { method: req.method, headers };

  if (req.method !== 'GET' && req.method !== 'HEAD') {
    fetchOptions.body = await req.text();
  }

  let backendRes: Response;
  try {
    backendRes = await fetch(backendUrl, fetchOptions);
  } catch {
    return NextResponse.json({ error: 'Backend unreachable.' }, { status: 503 });
  }

  if (backendRes.status === 401) {
    const response = NextResponse.json({ error: 'Session expired.' }, { status: 401 });
    response.cookies.set('auth_token', '', { maxAge: 0, path: '/' });
    response.cookies.set('user_info', '', { maxAge: 0, path: '/' });
    return response;
  }

  if (backendRes.status === 204) {
    return new NextResponse(null, { status: 204 });
  }

  const text = await backendRes.text();
  if (!text) {
    return new NextResponse(null, { status: backendRes.status });
  }

  const responseData = JSON.parse(text);
  return NextResponse.json(responseData, { status: backendRes.status });
}

type RouteContext = { params: Promise<{ path: string[] }> };

export async function GET(req: NextRequest, ctx: RouteContext) {
  return proxyRequest(req, await ctx.params);
}
export async function POST(req: NextRequest, ctx: RouteContext) {
  return proxyRequest(req, await ctx.params);
}
export async function PUT(req: NextRequest, ctx: RouteContext) {
  return proxyRequest(req, await ctx.params);
}
export async function DELETE(req: NextRequest, ctx: RouteContext) {
  return proxyRequest(req, await ctx.params);
}
