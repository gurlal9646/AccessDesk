# AccessDesk

A small access-request and audit app built to show a modern .NET + Angular stack with an identity focus and an AI assistant. All data is fake demo data.

## Status
Week 1: the API, the Angular UI with Keycloak login (authorization code + PKCE), role-based screens and automated tests are working end to end. The AI assistant and CI are next.

## What it does
- Employees raise requests for access to systems (GitHub, Jira, Azure and so on).
- Managers approve or reject them, with a comment. Nobody can decide their own request, and a request can only be decided once.
- Admins can read the audit log. Every create and decision writes an audit entry.

## Stack
- API: ASP.NET Core (.NET 9), EF Core code-first, SQL Server (SQLite for local and tests)
- Web: Angular 20 (standalone components, signals, Reactive Forms, lazy routes), `angular-auth-oidc-client`
- Auth: OpenID Connect. Keycloak runs locally in Docker; the API validates JWTs and maps realm roles to role claims (`KeycloakRolesClaimsTransformation`)
- Tests: xUnit unit and `WebApplicationFactory` integration tests (20), Jasmine/Karma (5)
- Planned: SCIM-style `/Users` endpoint, AI policy assistant, GitHub Actions CI, Azure deploy

## Why Keycloak and Docker
- **Keycloak** is a real identity provider. Login, passwords, MFA and tokens are its job, so this app never stores a password. It also lets the demo use the same flow production apps use (OIDC with authorization code + PKCE), with roles that are checked server-side.
- **Docker** makes the identity provider and the database reproducible: `docker compose up` gives anyone the same Keycloak realm (roles, clients, demo users) in a minute, with nothing installed on the machine.

## API
| Method | Route | Who | Notes |
|---|---|---|---|
| GET | `/api/requests` | any signed-in user | employees see their own; managers and admins see all; `?status=Pending` |
| POST | `/api/requests` | any signed-in user | system 2-200 chars, justification 10-2000 chars |
| POST | `/api/requests/{id}/approve` | manager, admin | 404 unknown, 409 already decided, 403 own request |
| POST | `/api/requests/{id}/reject` | manager, admin | same rules |
| GET | `/api/audit` | admin | newest first, `?take=` up to 500 |

Roles: `employee`, `manager`, `admin` (admin includes manager rights). The UI hides what a role cannot do, but the API is what enforces it.

## Run it locally
1. Start Docker Desktop, then:
   ```
   cp .env.example .env        # then edit the passwords
   docker compose up -d keycloak
   ```
2. API (SQLite, no SQL Server needed):
   ```
   cd src/AccessDesk.Api
   set Database__Provider=Sqlite
   set "ConnectionStrings__Default=Data Source=accessdesk-dev.db"
   set ASPNETCORE_URLS=http://localhost:5080
   dotnet run
   ```
   (PowerShell: `$env:Database__Provider="Sqlite"` and so on.) To use SQL Server instead: `docker compose up -d sqlserver` and leave the provider at its default.
3. Angular:
   ```
   cd web
   npm install
   npm start
   ```
4. Open http://localhost:4200 and sign in with a demo user from `keycloak/accessdesk-realm.json`: `alice` (employee), `mia` (manager), `ann` (admin). Those passwords are for local development only.

## Tests
```
dotnet test                                   # 20 tests, no Docker needed
cd web && npx ng test --watch=false           # 5 tests
bash scripts/smoke-test.sh                    # real tokens from Keycloak against the running API
```

## Milestones
- [x] Solution scaffold, domain model, EF Core code-first context
- [x] Role-based endpoints, audit log, validation, 20 tests
- [x] Angular UI with OIDC login (authorization code + PKCE), role-aware screens
- [ ] EF Core migrations
- [ ] AI policy assistant (retrieval over policy documents)
- [ ] SCIM-style `/Users` endpoint
- [ ] CI with GitHub Actions, Azure deploy
- [ ] Architecture diagram and demo video
