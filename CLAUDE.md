# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Stock portfolio tracker demonstrating a .NET + React stack. A user records buy/sell
transactions; the backend fetches live market prices (Alpha Vantage) and FX rates, then
reports portfolio value, cost basis, and P&L converted to a chosen base currency.
See `docs/requirements.md` for the full functional/non-functional spec (FR-01..FR-21,
NFR-01..NFR-07) — it is the source of truth for intended behavior, including the
isolated, auto-expiring **demo account** mechanism.
See `docs/database-schema.md` for full database schema and relationships.
See `docs/architecture/architecture-cross-cutting.md`, `docs/architecture/architecture-external-integrations.md`, 
`docs/architecture/architecture-layers.md`, `docs/architecture/architecture-request-lifecycle.md`, 
`docs/architecture/architecture-tech-stack.md` for full architecture description in project.

Current state: backend has two vertical slices landed — **Authentication** (register/login,
JWT bearer, bcrypt hashing) and **Transactions CRUD** (add/edit/delete + filtered/sorted/
paged list, the first `[Authorize]` endpoints with per-user scoping). The full pipeline is
wired (MediatR + FluentValidation `ValidationBehaviour` + AutoMapper + global exception
middleware). The `WeatherForecast` sample has been removed. The React/TypeScript frontend is
not yet in the repo. See `docs/implementation-plan.md` for the roadmap and what's next.

## Commands

Run from the repository root. The solution file is `PortfolioApp.slnx` (the newer XML
`.slnx` format — most `dotnet` commands pick it up automatically).

```bash
dotnet build                              # build all projects
dotnet run --project src/PortfolioApp.API # run the API (http://localhost:5029, https://localhost:7028)
dotnet test                               # run all tests (unit + integration)

# Run a single test project
dotnet test tests/PortfolioApp.UnitTests
dotnet test tests/PortfolioApp.IntegrationTests

# Run a single test / filtered subset
dotnet test --filter "FullyQualifiedName~MyTestClass"
dotnet test --filter "Name=MyTestMethod"

# EF Core migrations (design-time tools live in Infrastructure; startup project is the API)
dotnet ef migrations add <Name> --project src/PortfolioApp.Infrastructure --startup-project src/PortfolioApp.API
dotnet ef database update --project src/PortfolioApp.Infrastructure --startup-project src/PortfolioApp.API
```

API docs in Development are served by **Scalar** at `/scalar/v1` (the launch URL), backed
by the built-in OpenAPI document at `/openapi/v1.json`.

## Architecture

Clean Architecture with one-directional dependencies. The dependency arrow points inward:
API → Application → Domain, and Infrastructure → Application/Domain. Domain depends on
nothing.

- **`src/PortfolioApp.Domain`** — entities, value objects, domain logic. No external
  dependencies. Everything else references it.
- **`src/PortfolioApp.Application`** — use cases via **MediatR** (CQRS request/handler
  pattern), **FluentValidation** for input rules, **AutoMapper** for DTO mapping. Defines
  interfaces (e.g. repositories, external-service ports) that Infrastructure implements.
- **`src/PortfolioApp.Infrastructure`** — implementations of Application interfaces:
  **EF Core** + **Npgsql** (PostgreSQL) for persistence, **Hangfire** (PostgreSQL storage)
  for scheduled jobs (price refresh, expiring demo sessions), and outbound clients for
  market-data / FX APIs.
- **`src/PortfolioApp.API`** — ASP.NET Core 10 Web API: controllers, JWT bearer auth,
  Serilog logging, OpenAPI/Scalar. Composition root — this is where DI is wired and the
  only project that references both Application and Infrastructure.

When adding a feature, the typical flow is: Domain entity/logic → Application
command/query + handler + validator (+ interface for any new external dependency) →
Infrastructure implementation → API controller endpoint.

## Tests

- **`tests/PortfolioApp.UnitTests`** — xUnit + FluentAssertions + NSubstitute (mocking) +
  AutoFixture (test data). References Application and Domain.
- **`tests/PortfolioApp.IntegrationTests`** — xUnit with `WebApplicationFactory`
  (`Microsoft.AspNetCore.Mvc.Testing`) driving the real API, and **Testcontainers** to spin
  up a disposable PostgreSQL instance — so Docker must be running for these to pass.


## Tech Stack
 
- **Backend:** ASP.NET Core 10 Web API, MediatR (CQRS), EF Core 10, PostgreSQL, Hangfire
- **Frontend:** React + TypeScript, Recharts
- **Auth:** JWT Bearer tokens
- **Market data:** Alpha Vantage API (prices + FX rates)
- **API docs:** Scalar UI at /scalar/v1

## Coding Conventions
 
- **Pattern:** CQRS via MediatR — one class per command/query in `Application/Features/<Feature>/`
- **Validation:** FluentValidation pipeline behaviour, validators live next to their command
- **Mapping:** AutoMapper profiles in `Application/Common/Mappings/`
- **Errors:** Custom exceptions in `Domain/Exceptions/`, caught by global middleware in API
- **Naming:** Commands end in `Command`, queries in `Query`, handlers in `Handler`, DTOs in `Dto`
- **Controllers:** Thin — only receive request, send to MediatR, return result
- **No business logic** in controllers or Infrastructure layer

## Key Domain Rules
 
- P&L calculated using **weighted average cost** method (not FIFO)
- All monetary values stored in **original transaction currency**; converted to user's base currency at display time using stored FX rates
- Demo session: isolated portfolio copy created on login, expires after 60 min, cleaned up by Hangfire job
- Demo JWT includes claims: `is_demo = true`, `demo_session_id`

## Do Not
 
- Do not put business logic in controllers or EF configurations
- Do not call external APIs (Alpha Vantage) directly from controllers — use interfaces in Application, implementations in Infrastructure
- Do not use `var` for non-obvious types
- Do not skip XML summary comments on public API controller actions