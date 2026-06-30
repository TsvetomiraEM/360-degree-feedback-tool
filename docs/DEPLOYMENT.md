# Deployment guide

Step-by-step instructions for hosting Feedback360 on **Render** or **Railway** using Docker. Do not commit secrets — set all sensitive values in the platform's environment UI.

## Prerequisites

- GitHub repository connected to Render or Railway
- A managed **PostgreSQL** database (included on both platforms)
- (Optional) Google OAuth client for SSO

## Environment variables

| Variable | Service | Description |
|----------|---------|-------------|
| `ConnectionStrings__Default` | API | PostgreSQL connection string from the platform |
| `Jwt__Secret` | API | Random string, **at least 32 characters** |
| `Jwt__Issuer` | API | e.g. `Feedback360` |
| `Jwt__Audience` | API | e.g. `Feedback360` |
| `Cors__Origins__0` | API | Public frontend URL (e.g. `https://feedback360.onrender.com`) |
| `Google__ClientId` | API | Google OAuth client ID (or placeholder) |
| `VITE_API_URL` | Frontend build | Public API URL (e.g. `https://feedback360-api.onrender.com`) |
| `VITE_GOOGLE_CLIENT_ID` | Frontend build | Same as Google client ID |

Copy from [`.env.example`](../.env.example) for local development; production values go in the host dashboard only.

## CORS

The API reads allowed origins from `Cors:Origins` in configuration. After deploying the frontend, add its HTTPS URL to `Cors__Origins__0` (and `__1` if you have a preview URL). Redeploy the API after changing CORS.

## Docker images

The repo includes:

- `backend/Dockerfile` — multi-stage .NET 8 API image (listens on port 8080)
- `frontend/Dockerfile` — Vite build + nginx (port 80)

Local full stack:

```bash
cp .env.example .env
docker compose up --build
```

## Render (recommended layout)

Deploy as **two web services + one PostgreSQL database**.

### 1. PostgreSQL

1. Create a **PostgreSQL** instance on Render.
2. Copy the **Internal Database URL** for the API service.

### 2. API web service

1. New **Web Service** → connect repo.
2. **Root directory:** `backend`
3. **Runtime:** Docker
4. **Health check path:** `/health`
5. Environment variables (see table above). Map the DB URL to `ConnectionStrings__Default`.
6. Deploy. Note the public URL (e.g. `https://feedback360-api.onrender.com`).

### 3. Frontend web service

1. New **Web Service** → same repo.
2. **Root directory:** `frontend`
3. **Runtime:** Docker
4. **Docker build args:**
   - `VITE_API_URL` = API public URL
   - `VITE_GOOGLE_CLIENT_ID` = your client ID or placeholder
5. Deploy. Note the public URL.

### 4. Finalize

1. Set `Cors__Origins__0` on the API to the frontend URL.
2. Redeploy API.
3. (Optional) Add Google authorized origins for the frontend URL.

## Railway

Railway can run the same Dockerfiles or use `docker-compose.yml` as a reference.

### Option A — Monorepo services

1. Create a **PostgreSQL** plugin.
2. Add a service from `backend/Dockerfile` (set root to `backend`).
3. Add a service from `frontend/Dockerfile` (set root to `frontend`).
4. Wire `ConnectionStrings__Default` from the Postgres plugin reference variable.
5. Set JWT, CORS, and Vite build args as on Render.
6. Expose API and frontend on public domains; point `VITE_API_URL` and CORS at those URLs.

### Option B — Docker Compose (local / private network)

```bash
docker compose up --build
```

For Railway compose deploys, map platform env vars into the `api` and `frontend` services in `docker-compose.yml` via `${VAR}` placeholders (same pattern as `DB_PASSWORD` today).

## Health checks

- **API:** `GET /health` → `{ "status": "healthy" }`
- Configure load balancers and Render/Railway health checks to use `/health` (not Swagger — Swagger is Development-only).

## Post-deploy smoke test

```bash
curl https://YOUR-API-URL/health
```

Sign in on the frontend with a seeded admin account (first deploy runs migrations + seed), then create a manager user and verify template creation.

## Live demo

A public demo URL is not bundled with this repository. After following the steps above, add your frontend URL to the README **Live Demo** section.
