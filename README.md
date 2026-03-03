
# Aura Wellness — Software Engineer Coding Assessment

## Objective

Design and build a working full-stack Minimum Viable Product (MVP) for a B2B multi-tenant SaaS platform. The platform supports company onboarding, multi-Business Unit (BU) management, and a custom cross-service chat integration. All code, design patterns, and architectural decisions are fully owned and can be explained and defended in a technical interview.

---

## Tech Stack & Deliverables

- **Frontend:** React (SPA, served via nginx)
- **Backend:** .NET 10 Web API (REST, JWT Auth)
- **Chat Service:** Golang (standalone microservice)
- **Databases:** PostgreSQL (main + chat), Redis (if enabled)
- **System Design Document:** See [docs/system-design.md](docs/system-design.md) for architecture diagrams, ER diagrams, and multi-tenancy/inter-service communication details.

---

## Business Requirements & Feature Mapping

1. **Company Onboarding**
   - Endpoint: `POST /api/companies/onboard` (or UI at `/onboard`)
   - Captures company name, address, contact number
   - Auto-generates default BU and Company Owner account (default password: `P@ssw0rd`)

2. **Data Isolation & Multi-Tenancy**
   - Shared schema with `company_id` discriminator
   - Global staff profile data (name, etc.) shared across BUs; BU-specific data (email, role) scoped per BU
   - All repository methods filter by `companyId` from JWT

3. **Role-Based Access Control (RBAC)**
   - Owner can create staff and assign roles (Owner, Admin, Staff)
   - Role is embedded in JWT and enforced via `[Authorize(Roles = ...)]`

4. **Automated Chat Provisioning & Admin Assignment**
   - On BU creation, backend calls Go Chat Service to provision a dedicated chat workspace
   - Company Owner is assigned as Chat Admin for each workspace

5. **Granular Chat Access Control**
   - Staff are added to chat workspace with `hasAccess: false` by default
   - Owner can grant chat access per staff, per BU via `PUT /api/chat/workspace/{buId}/members/{personId}/access`

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

---

## Services & Ports

| Service | Port | Description |
|---|---|---|
| Frontend (nginx + React SPA) | 3000 | Main UI; proxies `/api/*` to backend |
| Backend (.NET 10 Web API) | 5001 | REST API + JWT auth |
| Chat Service (Go) | 8081 | Internal chat workspace management |
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
  "ownerEmail": "alice@acme.com"
}
```
Creates: Company → default BU → Owner → Owner staff profile → Chat workspace for BU

### 2. Log in
`POST /api/auth/login` with `{ "email": "alice@acme.com", "password": "P@ssw0rd" }`

- If email is in one BU: returns JWT
- If in multiple BUs: returns 409 + BU choices; re-submit with `{ email, password, buId }`

### 3. Create a Business Unit
`POST /api/business-units` (Owner role required). Triggers chat workspace provisioning.

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
  └─ nginx (port 3000)
       ├─ Static React SPA
       └─ /api/* → backend:5001
                      ├─ EF Core → PostgreSQL (aura_wellness)
                      └─ HTTP (X-Internal-Key) → Go chat service:8081
                                                    └─ pgx → PostgreSQL (aura_chat)
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
| **Owner** | Everything: create BUs, create staff, change roles, grant chat access |
| **Admin** | Read staff list |
| **Staff** | Read own BU data |

---

## Chat Integration

- Chat workspaces are provisioned per BU via backend → Go service API call
- Company Owner is Chat Admin for each workspace
- Staff must be explicitly granted chat access per BU by Owner
- No real-time messaging; chat service manages workspaces and access only

---

## Default Password

All newly created accounts (onboarding owner + staff) use `P@ssw0rd` as the initial password. (Per assessment spec; in production, use invite-link and forced password change.)

---

## Assumptions & Design Decisions

1. **Single BU at staff creation** — Staff assigned to one BU at creation; can be added to more BUs in future.
2. **Chat access is opt-in** — Staff added to chat workspace with `hasAccess: false`; Owner must grant access.
3. **No real-time messaging** — Chat service manages workspaces and access only; messaging is out of scope.
4. **JWT in localStorage** — For assessment simplicity; production should use `httpOnly` cookies.
5. **Onboarding transaction boundary** — DB writes in one EF Core transaction; Go chat calls after commit. If Go service is down, HTTP 500 is returned but company/user records are retained.
6. **No distributed rollback** — No compensating transaction strategy (per assessment scope).

---

## Submission Guidelines & Expectations

- **Timeframe:** Submit within 7 days of receiving the assessment.
- **Running the Application:**
  - A `docker-compose.yml` is provided at the root.
  - Run all services (frontend, backend, chat, databases) with a single `docker-compose up` command.
- **Documentation:**
  - This README explains architecture, setup, and key assumptions.
  - See [docs/system-design.md](docs/system-design.md) for technical diagrams and multi-tenancy/inter-service details.

