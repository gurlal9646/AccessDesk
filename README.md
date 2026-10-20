# AccessDesk

A small access-request and audit app built to show a modern .NET + Angular stack with an identity focus and an AI assistant. All data is fake demo data.

## Status
Week 1 in progress. The API, domain rules, role-based authorization and 20 automated tests are in place. The Angular UI, the login flow and the AI assistant are next.

## What it does
- Employees raise requests for access to systems (GitHub, Jira, Azure and so on).
- Managers approve or reject them, with a comment. Nobody can decide their own request, and a request can only be decided once.
- Admins can read the audit log. Every create and decision writes an audit entry.

## Stack
- API: ASP.NET Core (.NET 9), EF Core code-first, SQL Server (SQLite for tests)
- Web: Angular 20 (standalone components, signals, Reactive Forms), planned
- Auth: OpenID Connect via JWT bearer. Keycloak runs locally in Docker; realm roles are mapped to role claims (`KeycloakRolesClaimsTransformation`)
- Tests: xUnit, `WebApplicationFactory` integration tests, in-memory SQLite
- Planned: SCIM-style `/Users` endpoint, AI policy assistant (retrieval over policy documents), GitHub Actions CI, Azure deploy

## API
| Method | Route | Who | Notes |
|---|---|---|---|
| GET | `/api/requests` | any signed-in user | employees see their own; managers and admins see all; `?status=Pending` |
| POST | `/api/requests` | any signed-in user | validated: system 2-200 chars, justification 10-2000 chars |
| POST | `/api/requests/{id}/approve` | manager, admin | 404 unknown, 409 already decided, 403 own request |
| POST | `/api/requests/{id}/reject` | manager, admin | same rules |
| GET | `/api/audit` | admin | newest first, `?take=` up to 500 |

Roles: `employee`, `manager`, `admin` (admin includes manager rights).

## Run the tests
```
dotnet test
```
The tests need no Docker and no database server.

## Run locally with Keycloak and SQL Server
```
cp .env.example .env        # then edit the passwords
docker compose up -d
dotnet run --project src/AccessDesk.Api
```
Keycloak imports the `accessdesk` realm (roles `employee`, `manager`, `admin`, and three demo users) from `keycloak/accessdesk-realm.json`. Demo passwords in that file are for local development only. The compose file and realm have not yet been exercised end to end with the Angular app.

## Milestones
- [x] Solution scaffold, domain model, EF Core code-first context
- [x] Role-based endpoints, audit log, validation, 20 tests
- [ ] Angular UI with OIDC login (authorization code + PKCE)
- [ ] EF Core migrations
- [ ] AI policy assistant (retrieval over policy documents)
- [ ] SCIM-style `/Users` endpoint
- [ ] CI with GitHub Actions, Azure deploy
- [ ] Architecture diagram and demo video
