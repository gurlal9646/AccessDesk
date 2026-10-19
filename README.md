# AccessDesk

A small access-request and audit app built to show a modern .NET + Angular stack with an identity focus and an AI assistant. All data is fake demo data.

## Status
Day 1 scaffold. See the milestones below.

## Stack
- API: ASP.NET Core (.NET 9), EF Core, SQL Server
- Web: Angular 20 (standalone components, signals, Reactive Forms)
- Auth: OpenID Connect login, role-based policies
- Provisioning: minimal SCIM-style /Users endpoint
- AI: policy Q&A with retrieval over sample policy documents (Claude API)
- Tests: xUnit, Angular unit tests
- CI/CD: GitHub Actions, Azure App Service

## Milestones
- [ ] Week 1: OIDC login, role policies, first endpoints with tests
- [ ] Week 2: access-request workflow, audit log, first AI call
- [ ] Week 3: AI chat UI, SCIM-style /Users, CI, Azure deploy
- [ ] Week 4: README polish, architecture diagram, demo video

## Run locally
```
dotnet run --project src/AccessDesk.Api
cd web && npm start
```
