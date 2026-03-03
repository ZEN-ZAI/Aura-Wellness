import { NextRequest, NextResponse } from 'next/server';
import type { AuthUser } from '@/domain/entities/AuthUser';
import type { BuChoice } from '@/domain/entities/BuChoice';

const BACKEND_URL = process.env.BACKEND_URL ?? 'http://localhost:5239';
const COOKIE_MAX_AGE = 60 * 60; // 1 hour, matches Jwt:ExpiryMinutes

interface BuChoiceResponse {
  requiresBuSelection: true;
  choices: BuChoice[];
}

interface LoginResponse {
  token: string;
  personId: string;
  buId: string;
  companyId: string;
  role: string;
  firstName: string;
  lastName: string;
}

function cookieOptions(maxAge: number) {
  return {
    httpOnly: true,
    secure: process.env.NODE_ENV === 'production',
    sameSite: 'lax' as const,
    path: '/',
    maxAge,
  };
}

export async function POST(req: NextRequest) {
  const body = await req.json();

  let backendRes: Response;
  try {
    backendRes = await fetch(`${BACKEND_URL}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
  } catch {
    return NextResponse.json({ error: 'Backend unreachable.' }, { status: 503 });
  }

  const data = await backendRes.json();

  if (!backendRes.ok) {
    return NextResponse.json({ error: data.error ?? 'Invalid credentials.' }, { status: backendRes.status });
  }

  // Phase 1: multiple BUs — user must select one
  if (data.requiresBuSelection) {
    return NextResponse.json(data as BuChoiceResponse, { status: 200 });
  }

  // Phase 2: successful login with token
  const loginData = data as LoginResponse;
  const { token, ...rest } = loginData;

  const user: AuthUser = {
    personId: rest.personId,
    buId: rest.buId,
    companyId: rest.companyId,
    role: rest.role as AuthUser['role'],
    firstName: rest.firstName,
    lastName: rest.lastName,
  };

  const response = NextResponse.json({ user }, { status: 200 });
  response.cookies.set('auth_token', token, cookieOptions(COOKIE_MAX_AGE));
  response.cookies.set('user_info', JSON.stringify(user), cookieOptions(COOKIE_MAX_AGE));

  return response;
}
