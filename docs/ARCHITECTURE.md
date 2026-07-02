# Architecture

Feedback360 is a full-stack 360-degree feedback platform built with **Clean Architecture** on ASP.NET Core 8 and a React + TypeScript SPA.

## System overview

```mermaid
flowchart TB
    subgraph Client
        SPA[React SPA<br/>Material UI]
    end

    subgraph Docker["Docker Compose"]
        FE[nginx frontend :3000]
        API[ASP.NET Core API :5000]
        DB[(PostgreSQL 16)]
    end

    subgraph External
        Google[Google OAuth]
    end

    SPA -->|HTTPS / REST| FE
    FE -->|proxy / VITE_API_URL| API
    API --> DB
    SPA -.->|optional SSO| Google
    API -.->|token validation| Google
```

## Clean Architecture layers

```mermaid
flowchart TB
    subgraph Api["Feedback360.Api"]
        Controllers[Controllers]
        Program[Program / middleware]
    end

    subgraph Application["Feedback360.Application"]
        Services[Services]
        DTOs[DTOs]
        Interfaces[ICurrentUserService, IApplicationDbContext]
    end

    subgraph Domain["Feedback360.Domain"]
        Entities[Entities]
        Enums[Enums]
    end

    subgraph Infrastructure["Feedback360.Infrastructure"]
        EF[EF Core / AppDbContext]
        Auth[JWT, Google, PasswordHasher]
        Seed[SeedData]
    end

    Controllers --> Services
    Services --> Interfaces
    Services --> Entities
    Infrastructure --> Interfaces
    Infrastructure --> Entities
    EF --> Entities
```

| Layer | Responsibility |
|-------|----------------|
| **Domain** | Entities, enums, no dependencies |
| **Application** | Business logic, DTOs, service orchestration |
| **Infrastructure** | EF Core, auth, external integrations |
| **Api** | HTTP surface, Swagger, CORS, DI composition |

## Authentication flow

```mermaid
sequenceDiagram
    participant U as User / Browser
    participant FE as React SPA
    participant API as Auth API
    participant DB as PostgreSQL

    U->>FE: Email + password
    FE->>API: POST /api/v1/auth/login
    API->>DB: Validate user + password hash
    DB-->>API: User record
    API-->>FE: JWT access token + user DTO
    FE->>FE: Store token (localStorage)
    FE->>API: Authorized requests (Bearer header)
    API->>API: JWT validation + role claims

    Note over U,API: Google SSO (optional)
    U->>FE: Google sign-in button
    FE->>API: POST /api/v1/auth/google { idToken }
    API->>API: Validate Google token, match pre-provisioned email
    API-->>FE: JWT + user DTO
```

## RBAC matrix

| Capability | Admin | Manager | Employee |
|------------|:-----:|:-------:|:--------:|
| Manage users | ✓ | ✗ | ✗ |
| View audit logs | ✓ | ✗ | ✗ |
| Create/edit templates | ✗ | ✓ | ✗ |
| Create surveys & assign peers | ✗ | ✓ | ✗ |
| Respond to assignments | ✗ | ✓ | ✓ |
| View team results | ✗ | ✓ | ✗ |
| View own published results | ✗ | ✗ | ✓ |
| Access `/results` API | ✗ (403) | ✓ | ✓ (own only) |

Enforcement happens in two places:

1. **API** — `[Authorize(Roles = ...)]` on controllers plus service-level checks (e.g. admins blocked from results in `ResultsService`).
2. **Frontend** — route guards (`AdminOnly`, `ManagerOnly`, `ResultsViewer`) redirect unauthorized navigation.

## Survey lifecycle

```mermaid
stateDiagram-v2
    [*] --> Draft: Manager creates survey
    Draft --> Active: Assign peers / self / manager reviewers
    Active --> Active: Reviewers submit responses
    Active --> Closed: Manager publishes results
    Closed --> [*]: Optional delete (manager owner)

    note right of Closed
        Employee can view results
        when ResultsPublished = true
    end note
```

Typical flow:

1. Manager builds or picks a **template** with categorized questions.
2. Manager launches a **survey** for a direct report.
3. **Assignments** are created for self, manager, and peer reviewers.
4. Reviewers complete responses via the assignments UI.
5. Manager **publishes results**; employee and manager can view charts and comments.

## User delete cascade

When an admin deletes a user, `UserService.DeleteAsync` runs a single transaction:

```mermaid
flowchart TD
    A[Admin DELETE /admin/users/:id] --> B{Last admin?}
    B -->|yes| X[409 Conflict]
    B -->|no| C[Clear ManagerId on direct reports]
    C --> D[Delete surveys where user is subject]
    D --> E[Delete surveys created by user]
    E --> F[Delete reviewer assignments + responses]
    F --> G[Delete owned templates + template shares]
    G --> H[Remove user row]
    H --> I[Write audit log]
```

## Testing strategy

```mermaid
flowchart LR
    subgraph Unit["Unit tests"]
        AppTests[Application.Tests<br/>xUnit + InMemory EF]
    end

    subgraph Integration["API integration"]
        ApiTests[Api.Tests<br/>WebApplicationFactory]
    end

    subgraph E2E["End-to-end"]
        PW[Playwright<br/>docker compose stack]
    end

    AppTests -->|80% line coverage| Services[Application + Infrastructure services]
    ApiTests -->|HTTP + auth + RBAC| API[Feedback360.Api]
    PW -->|smoke flows| Full[Browser + API + DB]
```

| Suite | Location | What it validates |
|-------|----------|-------------------|
| Application unit | `backend/tests/Feedback360.Application.Tests` | Service logic, edge cases, coverage gate |
| API integration | `backend/tests/Feedback360.Api.Tests` | Login, RBAC, categories, delete cascade, `/health` |
| E2E smoke | `e2e/tests` | Manager login, admin blocked from results, template CRUD UI |

## API reference

Interactive OpenAPI UI: `http://localhost:5000/swagger` (Development only).

To export the OpenAPI document locally:

```bash
cd backend/src/Feedback360.Api
dotnet run &
curl -s http://localhost:5000/swagger/v1/swagger.json -o ../../docs/swagger.json
```

See also the [API Overview](../README.md#api-overview) in the README.
