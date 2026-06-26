# Feedback360 — 360-Degree Feedback Platform

A full-stack 360-degree feedback application with role-based access, Google SSO, survey templates, and Workday-inspired UX.

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

Backend unit tests use xUnit with EF Core InMemory and enforce ~80% line coverage on Application and Infrastructure service code.

```bash
cd backend
dotnet test tests/Feedback360.Application.Tests -p:CollectCoverage=true
```

This enforces a minimum **80% line coverage** on Application and Infrastructure service code (excluding migrations, seed data, and startup wiring). Coverage reports are written to `backend/tests/Feedback360.Application.Tests/coverage.cobertura.xml`.

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


| Area | Base Path |
|------|-----------|
| Auth | `/api/v1/auth` |
| Admin Users | `/api/v1/admin/users` |
| Audit Logs | `/api/v1/admin/audit-logs` |
| Templates | `/api/v1/templates` |
| Categories | `/api/v1/categories` |
| Surveys | `/api/v1/surveys` |
| Assignments | `/api/v1/assignments` |
| Results | `/api/v1/results` |
