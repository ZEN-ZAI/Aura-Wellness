// Lightweight stub for next/server used in Jest
export class NextRequest {
  url: string;
  method: string;
  private _body: string;

  constructor(url: string, init: { method?: string; body?: string } = {}) {
    this.url = url;
    this.method = init.method ?? 'GET';
    this._body = init.body ?? '';
  }

  async text() {
    return this._body;
  }

  async json() {
    return JSON.parse(this._body);
  }
}

export class NextResponse {
  status: number;
  private _body: unknown;
  cookies = { set: jest.fn(), get: jest.fn() };
  headers = new Map<string, string>();

  constructor(body: unknown, init: { status?: number } = {}) {
    this._body = body;
    this.status = init.status ?? 200;
  }

  async json() {
    return this._body;
  }

  static json(data: unknown, init: { status?: number } = {}) {
    const r = new NextResponse(data, init);
    return r;
  }
}
