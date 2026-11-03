# AccessDesk

[![CI](https://github.com/gurlal9646/AccessDesk/actions/workflows/ci.yml/badge.svg)](https://github.com/gurlal9646/AccessDesk/actions/workflows/ci.yml)

A small access-request and audit app built to show a modern .NET + Angular stack with an identity focus and an AI assistant. All data is fake demo data.

## Status
Week 1: the API, the Angular UI with Keycloak login (authorization code + PKCE), role-based screens and automated tests are working end to end. CI runs on every push.

Week 2 (in progress): the AI policy assistant works end to end in code: the API and the Angular "Ask the policy" screen, both tested. A check with a real Claude API key is next.

## What it does
- Employees raise requests for access to systems (GitHub, Jira, Azure and so on).
- Managers approve or reject them, with a comment. Nobody can decide their own request, and a request can only be decided once.
- Admins can read the audit log. Every create and decision writes an audit entry.

## Stack
- API: ASP.NET Core (.NET 9), EF Core code-first with migrations, SQL Server (SQLite for local and tests)
- Web: Angular 20 (standalone components, signals, Reactive Forms, lazy routes), `angular-auth-oidc-client`
- Auth: OpenID Connect. Keycloak runs locally in Docker; the API validates JWTs and maps realm roles to role claims (`KeycloakRolesClaimsTransformation`)
- AI: policy assistant that retrieves sections of the policy documents and asks the Claude API to answer from them, with citations
- Tests: xUnit unit and `WebApplicationFactory` integration tests (41), Jasmine/Karma (10)
- Planned: SCIM-style `/Users` endpoint, Azure deploy

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
| POST | `/api/assistant/ask` | any signed-in user | question 5-500 chars; returns `answer` and `sources`; 503 if the model is not configured or unreachable |

Roles: `employee`, `manager`, `admin` (admin includes manager rights). The UI hides what a role cannot do, but the API is what enforces it.

## AI policy assistant
The policy documents are markdown files in `src/AccessDesk.Api/Policies`. They are fictional demo content.
1. **Retrieve:** each `##` section is a chunk. A question is scored against the chunks with TF-IDF keyword scoring (heading matches count triple) and the top 3 are kept. It is not embeddings: the document set is small, and keyword scoring is deterministic and easy to test.
2. **Ask:** the chunks and the question go to the Claude Messages API in one request. The system prompt tells the model to answer only from the numbered excerpts, cite them, say so when the policy does not cover the question, and treat the question as a question rather than an instruction.
3. **Skip the model when there is nothing to say:** if no section matches, the API answers "not covered" without calling the model, which saves cost and avoids a made-up answer.

The API key is read from `Assistant:ApiKey`. Set it as an environment variable, never in a file that is committed:
```
set Assistant__ApiKey=your-key        # PowerShell: $env:Assistant__ApiKey="your-key"
```
Tests and CI use a fake client, so they need no key and make no network calls.


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
   With SQL Server the API applies migrations on startup. If you ran an earlier version that used `EnsureCreated`, drop that database first (`docker compose down -v`), because its tables have no migration history.
3. Angular:
   ```
   cd web
   npm install
   npm start
   ```
4. Open http://localhost:4200 and sign in with a demo user from `keycloak/accessdesk-realm.json`: `alice` (employee), `mia` (manager), `ann` (admin). Those passwords are for local development only.

## Tests
```
dotnet test                                   # 23 tests, no Docker needed
cd web && npx ng test --watch=false           # 5 tests
bash scripts/smoke-test.sh                    # real tokens from Keycloak against the running API
```

## Milestones
- [x] Solution scaffold, domain model, EF Core code-first context
- [x] Role-based endpoints, audit log, validation, 23 tests, optimistic concurrency on decisions
- [x] Angular UI with OIDC login (authorization code + PKCE), role-aware screens
- [x] EF Core migrations (SQL Server; SQLite is created from the model)
- [ ] AI policy assistant (retrieval over policy documents)
- [ ] SCIM-style `/Users` endpoint
- [x] CI with GitHub Actions (API tests, Angular tests and build, migrations on SQL Server)
- [ ] Azure deploy
- [ ] Architecture diagram and demo video

## Changing the model
Migrations target SQL Server. After changing an entity:
```
dotnet tool restore
dotnet ef migrations add <Name> --project src/AccessDesk.Api --output-dir Data/Migrations
```
A test fails if the model and the migrations disagree.
