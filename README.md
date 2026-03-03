# Aura Wellness — Software Engineer Coding Assessment

A full-stack B2B multi-tenant SaaS platform built as a coding assessment.

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

All five services start automatically. The backend applies EF Core migrations and the Go chat service runs golang-migrate on startup — no manual DB setup required.

---

## Services & Ports

| Service | Port | Description |
|---|---|---|
| Frontend (nginx + React SPA) | 3000 | Main UI; proxies `/api/*` to backend |
| Backend (.NET 10 Web API) | 5001 | REST API + JWT auth |
| Chat Service (Go + Gin) | 8081 | Internal chat workspace management |
| PostgreSQL — main | (internal) | `aura_wellness` DB |
| PostgreSQL — chat | (internal) | `aura_chat` DB |

---

## Environment Variables

Defined in `.env` (copy from `.env.example`):

| Variable | Description | Example |
|---|---|---|
| `POSTGRES_MAIN_PASSWORD` | Password for the main PostgreSQL instance | `changeme` |
| `POSTGRES_CHAT_PASSWORD` | Password for the chat PostgreSQL instance | `changeme` |
| `JWT_SECRET` | HMAC-SHA256 secret for signing JWTs (≥ 32 chars) | `super-secret-jwt-key-change-me` |
| `INTERNAL_API_KEY` | Shared key protecting Go service endpoints | `internal-api-key-change-me` |

---

## Walkthrough

### 1. Onboard a company
`POST http://localhost:5001/api/companies/onboard` (or use the UI at `/onboard`)

```json
{
  "companyName": "Acme Corp",
  "address": "123 Main St",
  "contactNumber": "555-0100",
  "ownerFirstName": "Alice",
  "ownerLastName": "Smith",
  "ownerEmail": "alice@acme.com"
}
```

This creates: Company → default Business Unit ("{CompanyName} HQ") → Owner person → Owner staff profile → Chat workspace for the default BU.

### 2. Log in
`POST /api/auth/login` with `{ "email": "alice@acme.com", "password": "P@ssw0rd" }`

- If the email appears in exactly one BU → returns a JWT immediately.
- If the email appears in multiple BUs → returns HTTP 409 with a list of BU choices; re-submit with `{ email, password, buId }`.

### 3. Create a Business Unit
`POST /api/business-units` (Owner role required). A chat workspace is automatically provisioned for every new BU.

### 4. Create Staff
`POST /api/staff` (Owner role required). The staff member is added to the BU's chat workspace with `hasAccess: false` by default.

### 5. Grant Chat Access
`PUT /api/chat/workspace/{buId}/members/{personId}/access` (Owner role required):
```json
{ "hasAccess": true }
```

---

## Architecture Summary

```
Browser
  └─ nginx (port 3000)
       ├─ Static React SPA
       └─ /api/* → backend:8080
                      ├─ EF Core → PostgreSQL (aura_wellness)
                      └─ HTTP (X-Internal-Key) → Go chat service:8080
                                                    └─ pgx → PostgreSQL (aura_chat)
```

See [docs/system-design.md](docs/system-design.md) for full ER diagrams, sequence diagrams, and architectural trade-offs.

---

## Multi-Tenancy

Shared schema with a `company_id` discriminator. All repository methods always filter by the `companyId` claim extracted from the JWT — tenants are fully isolated at the application layer.

---

## RBAC

| Role | Can do |
|---|---|
| **Owner** | Everything: create BUs, create staff, change roles, grant chat access |
| **Admin** | Read staff list |
| **Staff** | Read own BU data |

Role is embedded in the JWT and enforced via `[Authorize(Roles = "...")]` attributes.

---

## Default Password

All newly created accounts (onboarding owner + staff created by Owner) use `P@ssw0rd` as the initial password. This is intentional per the assessment spec and clearly documented here.

> **Security note:** In a production system this would be replaced by an invite-link flow with forced password change on first login.

---

## Assumptions & Design Decisions

1. **Single BU at staff creation** — A staff member is assigned to one BU when created. They can be added to additional BUs via future extension.
2. **Chat access is opt-in** — Staff are added to the chat workspace automatically but `hasAccess` defaults to `false`. The Owner must explicitly grant access.
3. **No real-time messaging** — The chat service manages workspaces and member access only; actual messaging is out of scope.
4. **JWT in localStorage** — Simple for the assessment. A production app should use `httpOnly` cookies to mitigate XSS risk.
5. **Onboarding transaction boundary** — Steps 1–5 (DB writes) run inside a single EF Core transaction. The Go chat calls (steps 6–7) happen after the commit. If the Go service is unavailable, the HTTP response is 500 but the company/user records are retained — the workspace can be re-provisioned.
6. **No distributed rollback** — Given the assessment scope, a compensating transaction strategy was not implemented.
