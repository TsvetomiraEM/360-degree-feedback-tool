# Feedback360

**Enterprise-style 360° feedback — surveys, peer reviews, and role-aware results in one full-stack app.**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18-61DAFB?logo=react&logoColor=black)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.x-3178C6?logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://docs.docker.com/compose/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

[![Backend Tests](https://github.com/TsvetomiraEM/360-degree-feedback-tool/actions/workflows/backend-tests.yml/badge.svg)](https://github.com/TsvetomiraEM/360-degree-feedback-tool/actions/workflows/backend-tests.yml)
[![Frontend CI](https://github.com/TsvetomiraEM/360-degree-feedback-tool/actions/workflows/frontend-ci.yml/badge.svg)](https://github.com/TsvetomiraEM/360-degree-feedback-tool/actions/workflows/frontend-ci.yml)
[![E2E Tests](https://github.com/TsvetomiraEM/360-degree-feedback-tool/actions/workflows/e2e.yml/badge.svg)](https://github.com/TsvetomiraEM/360-degree-feedback-tool/actions/workflows/e2e.yml)

## Problem → Solution

**Problem:** HR and engineering managers need structured 360° feedback — multiple reviewer types, categorized questions, and controlled visibility — without spreadsheets or ad-hoc forms.

**Solution:** Feedback360 provides a Workday-inspired web app where managers launch surveys from templates, peers and managers respond online, and results publish only when ready. Admins manage users and audit trails but **cannot** see survey content, keeping separation of duties.

| | |
|---|---|
| ![Dashboard placeholder](docs/images/placeholder-dashboard.svg) | ![Templates placeholder](docs/images/placeholder-templates.svg) |

## What I built

- **Clean Architecture** backend (Domain → Application → Infrastructure → API) with JWT auth and optional Google SSO
- **Role-based access** for Admin, Manager, and Employee with API + UI enforcement
- **Survey lifecycle** — templates, categorized questions, assignments, responses, published results with charts
- **Admin tooling** — user CRUD with cascading delete of all related 360 data, audit log viewer
- **Quality gates** — ~80% unit test coverage, API integration tests, Playwright E2E smoke tests, GitHub Actions CI

## Live Demo

**Coming soon** — a public deployment is not hosted yet. To run locally or deploy yourself, see [Quick Start](#quick-start-docker) and [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md).

## Stack

- **Backend:** ASP.NET Core 8 (Clean Architecture)
- **Frontend:** React 18 + TypeScript + Material UI
- **Database:** PostgreSQL 16
- **Containers:** Docker Compose (api, frontend, db)

## Roles

| Role | Capabilities |
|------|-------------|
| **Admin** | Create/edit/delete users, view audit logs. **Cannot** access surveys or results. |
| **Manager** | Create surveys, assign peers, respond, view/share results for direct reports, manage/share templates. |
| **Employee** | Respond to assigned surveys, view own published 360 results. |

## Quick Start (Docker)

```bash
cp .env.example .env
docker compose up --build
```

- Frontend: http://localhost:3000
- API / Swagger: http://localhost:5000/swagger
- Health: http://localhost:5000/health

## Local Development

### Database

```bash
docker compose up db -d
```

### Backend

```bash
cd backend/src/Feedback360.Api
dotnet run
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

## Running Tests

### Backend unit tests

```bash
cd backend
dotnet test tests/Feedback360.Application.Tests -p:CollectCoverage=true
```

Enforces **80% line coverage** on Application and Infrastructure services. Reports: `backend/tests/Feedback360.Application.Tests/coverage.cobertura.xml`.

### API integration tests

```bash
cd backend
dotnet test tests/Feedback360.Api.Tests
```

Covers login, RBAC (admin forbidden from results), category deduplication, user delete cascade, and `/health`.

### E2E tests (Playwright)

Requires the Docker stack running on ports 3000 and 5000:

```bash
docker compose up --build -d
cd e2e
npm install
npx playwright install chromium
npm test
```

Optional UI mode: `npm run test:ui`. Override base URL with `E2E_BASE_URL`.

## Seed Accounts

| Email | Password | Role |
|-------|----------|------|
| admin@feedback360.local | Admin123! | Admin |
| manager@feedback360.local | Manager123! | Manager |
| alice@feedback360.local | Employee123! | Employee |
| bob@feedback360.local | Employee123! | Employee |

## Google SSO Setup

1. Create an OAuth 2.0 Client ID in [Google Cloud Console](https://console.cloud.google.com/).
2. Add authorized JavaScript origins: `http://localhost:3000`, `http://localhost:5173`
3. Set `GOOGLE_CLIENT_ID` in `.env` and `VITE_GOOGLE_CLIENT_ID` for the frontend.
4. Pre-provision users in the admin panel with matching email addresses.

## API Overview

| Area | Base Path |
|------|-----------|
| Health | `/health` |
| Auth | `/api/v1/auth` |
| Admin Users | `/api/v1/admin/users` |
| Audit Logs | `/api/v1/admin/audit-logs` |
| Templates | `/api/v1/templates` |
| Categories | `/api/v1/categories` |
| Surveys | `/api/v1/surveys` |
| Assignments | `/api/v1/assignments` |
| Results | `/api/v1/results` |

OpenAPI (Development): http://localhost:5000/swagger — export instructions in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Architecture

System design, auth flow, RBAC matrix, survey lifecycle, and testing strategy:

**[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)**

## Deployment

Render / Railway step-by-step guide (env vars, CORS, Docker):

**[docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)**

## User Deletion

When an admin deletes a user, all related 360 data is permanently removed in a single transaction:

- Surveys where the user is the subject
- Surveys created by the user
- All assignments and responses as a reviewer
- Owned templates and template shares
- Direct reports have their manager reference cleared

## Question Categories

Managers can assign each question to a category (e.g. Skills, Performance, Leadership). Categories are organization-wide: once created by any manager, they appear in the shared list for all managers when building templates or surveys.

Default seeded categories: Skills, Performance, Leadership, Teamwork, Communication.

## Security

- **Authentication:** JWT bearer tokens (local login + optional Google ID token validation)
- **Authorization:** ASP.NET Core role policies on controllers; additional checks in services (e.g. admins blocked from results)
- **Passwords:** Hashed at rest; never logged or returned in API responses
- **CORS:** Explicit allow-list from configuration (`Cors:Origins`)
- **Secrets:** Use environment variables in production — see `.env.example` and [DEPLOYMENT.md](docs/DEPLOYMENT.md). Do not commit `.env` files.
- **Audit trail:** Admin actions on users are recorded in audit logs
- **Health:** Unauthenticated `GET /health` for orchestration probes only (no sensitive data)

## For reviewers

Suggested path to evaluate this project (~15 minutes):

1. Read [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for design decisions and diagrams.
2. `docker compose up --build` — open http://localhost:3000.
3. Sign in as **manager@feedback360.local** / `Manager123!` — create a template, launch a survey.
4. Sign in as **admin@feedback360.local** — confirm Users and Audit Logs work; try `/results` (redirected away).
5. Run `cd backend && dotnet test` and skim `backend/tests/` for coverage and integration scenarios.
6. (Optional) `cd e2e && npm test` after the stack is up.

Key files: `backend/src/Feedback360.Application/Services/`, `frontend/src/routes/guards.tsx`, `backend/tests/Feedback360.Api.Tests/`.

## Screenshots

Placeholder wireframes live in [docs/images/](docs/images/). See [docs/images/README.md](docs/images/README.md) for capture instructions.

## License

[MIT](LICENSE)
