
# Aura Wellness — Software Engineer Coding Assessment

## Objective

Design and build a working full-stack Minimum Viable Product (MVP) for a B2B multi-tenant SaaS platform. The platform supports company onboarding, multi-Business Unit (BU) management, and a custom cross-service chat integration with real-time messaging. All code, design patterns, and architectural decisions are fully owned and can be explained and defended in a technical interview.

---

## Tech Stack & Deliverables

- **Frontend:** Next.js 16 (React 19, TypeScript, Ant Design, Zustand, Tailwind CSS v4)
- **Backend:** .NET 10 Web API (Clean Architecture, EF Core, JWT Auth, gRPC client)
- **Chat Service:** Golang (standalone gRPC microservice with WebSocket support for real-time messaging)
- **Databases:** PostgreSQL 16 (main + chat — separate instances), Redis 7 (pub/sub for real-time chat)
- **System Design Document:** See [docs/system-design.md](docs/system-design.md) for architecture diagrams, ER diagrams, and multi-tenancy/inter-service communication details.

---

## Business Requirements & Feature Mapping

1. **Company Onboarding**
   - Endpoint: `POST /api/companies/onboard` (or UI at `/onboard`)
   - Captures company name, address, contact number
   - Auto-generates default BU and Company Owner account (password set by user during registration)

2. **Data Isolation & Multi-Tenancy**
   - Shared schema with `company_id` discriminator
   - Global staff profile data (name, etc.) shared across BUs; BU-specific data (email, role) scoped per BU
   - All repository methods filter by `companyId` from JWT

3. **Role-Based Access Control (RBAC)**
   - Owner can create staff and assign roles (Owner, Admin, Staff)
   - Role is embedded in JWT and enforced via `[Authorize(Roles = ...)]`

4. **Automated Chat Provisioning & Admin Assignment**
   - On BU creation, backend calls Go Chat Service via gRPC to provision a dedicated chat workspace
   - Company Owner is assigned as Chat Admin for each workspace

5. **Granular Chat Access Control**
   - Staff are added to chat workspace with `hasAccess: false` by default
   - Owner can grant chat access per staff, per BU via `PUT /api/chat/workspace/{buId}/members/{personId}/access`

6. **Real-Time Chat Messaging**
   - WebSocket-based real-time messaging per BU workspace
   - Messages persisted in PostgreSQL, streamed via Redis pub/sub
   - Frontend WebSocket connection proxied through Next.js custom server (JWT in httpOnly cookie)

---

## Quickstart

**Prerequisites:** Docker + Docker Compose

```bash
# 1. Clone / enter the project root
cd "Aura Wellness"

# 2. Copy the environment file
cp .env.example .env

# 3. Spin up everything
docker compose up --build

# 4. Open the app
open http://localhost:3000
```

All services start automatically. The backend applies EF Core migrations and the Go chat service runs golang-migrate on startup — no manual DB setup required.

### Demo Seed Account

After startup, a demo company is pre-seeded:
- **Email:** `Welcome@example.com`
- **Password:** `P@ssw0rd`

---

## Services & Ports

| Service | Port | Description |
|---|---|---|
| Frontend (Next.js + custom WS server) | 3000 | Main UI; BFF proxy for API + WebSocket |
| Backend (.NET 10 Web API) | 5001 | REST API + JWT auth + gRPC client |
| Chat Service (Go gRPC + WS) | 50051 / 8080 | gRPC for workspace management, WS for real-time chat |
| PostgreSQL — main | 15432 | `aura_wellness` DB |
| PostgreSQL — chat | 15433 | `aura_chat` DB |
| Redis | 6379 | Chat message pub/sub |

---

## Environment Variables

Defined in `.env` (copy from `.env.example`):

| Variable | Description | Example |
|---|---|---|
| `POSTGRES_MAIN_PASSWORD` | Password for the main PostgreSQL instance | `changeme_main` |
| `POSTGRES_CHAT_PASSWORD` | Password for the chat PostgreSQL instance | `changeme_chat` |
| `JWT_SECRET` | HMAC-SHA256 secret for signing JWTs (≥ 32 chars) | `super_secret_jwt_key_minimum_32_characters_long` |
| `INTERNAL_API_KEY` | Shared key protecting Go gRPC service | `internal_service_shared_secret_key` |
| `DEFAULT_STAFF_PASSWORD` | Default password for new staff accounts | `P@ssw0rd` |

---

## API Walkthrough

### 1. Onboard a Company
`POST http://localhost:5001/api/companies/onboard`

```json
{
  "companyName": "Acme Corp",
  "address": "123 Main St",
  "contactNumber": "555-0100",
  "ownerFirstName": "Alice",
  "ownerLastName": "Smith",
  "ownerEmail": "alice@acme.com",
  "ownerPassword": "MySecureP@ss1"
}
```
Creates: Company → default BU → Owner person → Owner staff profile → Chat workspace for BU

### 2. Log in
`POST /api/auth/login` with `{ "email": "alice@acme.com", "password": "MySecureP@ss1" }`

- If email is in one BU: returns JWT (stored in httpOnly cookie via BFF)
- If in multiple BUs: returns 409 + BU choices; re-submit with `{ email, password, buId }`

### 3. Create a Business Unit
`POST /api/business-units` (Owner role required). Triggers chat workspace provisioning via gRPC.

### 4. Create Staff
`POST /api/staff` (Owner role required). Staff added to BU's chat workspace with `hasAccess: false`.

### 5. Grant Chat Access
`PUT /api/chat/workspace/{buId}/members/{personId}/access` (Owner role required):
```json
{ "hasAccess": true }
```

---

## Architecture Overview

```
Browser
  └─ Next.js custom server (port 3000)
       ├─ Server-side rendered React pages
       ├─ /api/proxy/* → backend:8080 (BFF with httpOnly cookie → Bearer token)
       ├─ /api/auth/*  → backend:8080 (login/logout with cookie management)
       └─ /api/chat/ws/:buId → chat-service:8080 (WebSocket proxy)
                      │
                      ▼
               .NET Backend (port 8080)
                      ├─ EF Core → PostgreSQL (aura_wellness)
                      ├─ gRPC → Go chat-service:50051
                      └─ Redis → Chat message streaming
                                      │
                                      ▼
                             Go Chat Service
                                ├─ gRPC :50051 (workspace/member management)
                                ├─ WS   :8080  (real-time message streaming)
                                ├─ pgx  → PostgreSQL (aura_chat)
                                └─ Redis pub/sub (message broadcast)
```

See [docs/system-design.md](docs/system-design.md) for full ER diagrams, sequence diagrams, and architectural trade-offs.

---

## Multi-Tenancy & Data Isolation

- Shared schema with `company_id` discriminator
- All repository methods filter by `companyId` from JWT
- Staff profile data (name, etc.) is global within company; BU-specific data (email, role) is per BU
- Tenants are fully isolated at the application layer

---

## Role-Based Access Control (RBAC)

| Role | Can do |
|---|---|
| **Owner** | Everything: create BUs, create staff, change roles, grant chat access, send/read chat |
| **Admin** | Read staff list, send/read chat (if access granted) |
| **Staff** | Read own BU data, send/read chat (if access granted) |

---

## Chat Integration

- Chat workspaces are provisioned per BU via backend → Go chat service gRPC call
- Company Owner is automatically assigned as Chat Admin for each workspace
- Staff must be explicitly granted chat access per BU by Owner
- Real-time messaging via WebSocket (persisted to PostgreSQL, streamed via Redis pub/sub)
- WebSocket connections are proxied through the Next.js custom server to inject the JWT from httpOnly cookies

---

## Security Design

- **JWT stored in httpOnly cookies** — not accessible to client-side JavaScript, mitigating XSS token theft
- **BFF (Backend-For-Frontend) proxy** — the frontend never exposes the JWT; all API calls route through server-side Next.js routes that inject the Authorization header
- **BCrypt password hashing** — cost factor 11
- **gRPC inter-service auth** — shared `x-internal-key` header validated by interceptors
- **WebSocket auth** — JWT validated server-side before upgrade; buId in token must match requested workspace

---

## Default Password

Newly created staff accounts (via `POST /api/staff`) use the `DEFAULT_STAFF_PASSWORD` environment variable as the initial password. Owners set their own password during onboarding. Users may change their password via the UI.

---

## Assumptions & Design Decisions

1. **Single BU at staff creation** — Staff assigned to one BU at creation; can be enrolled in additional BUs later via "Enroll in Another BU".
2. **Chat access is opt-in** — Staff added to chat workspace with `hasAccess: false`; Owner must explicitly grant access.
3. **Onboarding transaction boundary** — DB writes in one EF Core transaction; gRPC chat calls after commit. If chat service is down, HTTP 500 is returned but company/user records are retained.
4. **No distributed rollback** — No compensating transaction or saga pattern (per MVP scope).
5. **Multi-BU login** — If a user's email exists in multiple BUs, login returns a BU selection prompt (409). The user must choose which BU context to operate in.
6. **Owner password** — Set by the user during onboarding registration (not a default password).

---

## Submission Guidelines & Expectations

- **Timeframe:** Submit within 7 days of receiving the assessment.
- **Running the Application:**
  - A `docker-compose.yml` is provided at the root.
  - Run all services (frontend, backend, chat service, databases, Redis) with a single `docker compose up --build` command.
- **Documentation:**
  - This README explains architecture, setup, and key assumptions.
  - See [docs/system-design.md](docs/system-design.md) for technical diagrams and multi-tenancy/inter-service details.

