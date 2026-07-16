# Portfolio-.NET-React

A stock portfolio tracker demonstrating a **.NET + React** stack. A user records buy/sell
transactions; the backend fetches live market prices (Alpha Vantage) and FX rates, then
reports portfolio value, cost basis, and profit/loss converted to a chosen base currency.

> **Status:** backend under Clean Architecture with the persistence layer (PostgreSQL +
> EF Core, initial migration) and two vertical slices in place — **authentication**
> (register/login, JWT, bcrypt) and **transactions CRUD** (add/edit/delete + filtered,
> sorted, paged history, scoped per user). The React/TypeScript frontend (`frontend/`) is
> underway — API client, auth/session, routing, and the register/login screens are in;
> the transactions UI is next. See the [implementation plan](docs/implementation-plan.md)
> for the roadmap and [`frontend/README.md`](frontend/README.md) for the SPA.

## Features

- Record buy/sell **transactions** per portfolio
- **Live market prices** and **FX rates** from Alpha Vantage, refreshed on a schedule
- **Multi-currency** support — values converted to a chosen base currency at display time
- Portfolio summary: current value, cost basis, and **P&L** (weighted-average cost)
- **Charts**: value over time, allocation, per-asset price history, P&L history
- **JWT auth**, plus an isolated, auto-expiring **demo account** (60-minute sessions)

See [docs/requirements.md](docs/requirements.md) for the full functional/non-functional
spec (FR-01..FR-21, NFR-01..NFR-07) — the source of truth for intended behavior.

## Tech stack

- **Backend:** ASP.NET Core 10 Web API, MediatR (CQRS), EF Core 10, PostgreSQL, Hangfire
- **Frontend:** React 19 + TypeScript (Vite), React Router, TanStack Query, React Hook Form + Zod, axios; Recharts *(planned)*
- **Auth:** JWT bearer tokens
- **Market data:** Alpha Vantage API (prices + FX rates)
- **API docs:** Scalar UI at `/scalar/v1`

Full version-by-version breakdown:
[docs/architecture/architecture-tech-stack.md](docs/architecture/architecture-tech-stack.md).

## Architecture

Clean Architecture with one-directional, inward-pointing dependencies
(API → Application → Domain; Infrastructure → Application/Domain; Domain depends on
nothing):

- **`src/PortfolioApp.Domain`** — entities, value objects, domain logic. No external deps.
- **`src/PortfolioApp.Application`** — use cases via MediatR (CQRS), FluentValidation,
  AutoMapper. Defines the interfaces Infrastructure implements.
- **`src/PortfolioApp.Infrastructure`** — EF Core + Npgsql (PostgreSQL), Hangfire jobs,
  outbound market-data/FX clients.
- **`src/PortfolioApp.API`** — ASP.NET Core Web API and composition root (DI, JWT auth,
  Serilog, OpenAPI/Scalar).

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Docker](https://www.docker.com/) (for PostgreSQL locally and integration tests)

### Run locally

```bash
# 1. Start PostgreSQL + pgAdmin (copy .env.example to .env first)
docker compose up -d

# 2. Restore the local EF Core tool (dotnet-ef)
dotnet tool restore

# 3. Set the database connection string (user-secrets)
dotnet user-secrets set "ConnectionStrings:Default" "<your-connection-string>" --project src/PortfolioApp.API

# 4. Apply migrations
dotnet ef database update --project src/PortfolioApp.Infrastructure --startup-project src/PortfolioApp.API

# 5. Run the API
dotnet run --project src/PortfolioApp.API
```

The API listens on `http://localhost:5029` / `https://localhost:7028`. In Development,
interactive API docs (Scalar) are at `/scalar/v1`, backed by the OpenAPI document at
`/openapi/v1.json`. pgAdmin runs at `http://localhost:5050`.

### Build & test

```bash
dotnet build                              # build all projects
dotnet test                               # run all tests (Docker required for integration tests)
dotnet test tests/PortfolioApp.UnitTests  # unit tests only
```

## Documentation

All project documentation lives in [`docs/`](docs/):

| Document | What it covers |
|----------|----------------|
| [requirements.md](docs/requirements.md) | Functional & non-functional spec (FR/NFR) — source of truth for behavior |
| [database-schema.md](docs/database-schema.md) | Tables, columns, constraints, indexes, relationships (ER diagram) |
| [implementation-plan.md](docs/implementation-plan.md) | Build roadmap and the order features land in |

### Architecture ([`docs/architecture/`](docs/architecture/))

| Document | What it covers |
|----------|----------------|
| [architecture-layers.md](docs/architecture/architecture-layers.md) | The four Clean Architecture projects and dependency direction |
| [architecture-external-integrations.md](docs/architecture/architecture-external-integrations.md) | PostgreSQL, Hangfire storage, and the Alpha Vantage client |
| [architecture-request-lifecycle.md](docs/architecture/architecture-request-lifecycle.md) | How a request flows through the MediatR CQRS pipeline |
| [architecture-cross-cutting.md](docs/architecture/architecture-cross-cutting.md) | Auth, validation, error handling, logging, API docs |
| [architecture-tech-stack.md](docs/architecture/architecture-tech-stack.md) | Frameworks, libraries, and tooling with versions |

## Project structure

```
src/
  PortfolioApp.Domain/          # entities, enums, exceptions (no dependencies)
  PortfolioApp.Application/      # CQRS use cases, ports, validators, mappings
  PortfolioApp.Infrastructure/   # EF Core, Hangfire, external clients
  PortfolioApp.API/             # ASP.NET Core Web API (composition root)
tests/
  PortfolioApp.UnitTests/       # xUnit + FluentAssertions + NSubstitute + AutoFixture
  PortfolioApp.IntegrationTests/ # WebApplicationFactory + Testcontainers (PostgreSQL)
frontend/                       # React + TypeScript SPA (Vite) — see frontend/README.md
  src/
    api/                        # typed API client (axios), request/response types, errors
    auth/                       # auth context + session (token/user in localStorage)
    routes/                     # React Router paths, guards, authenticated layout shell
    pages/                      # route screens (Login, Register, Transactions)
      auth/                     # Zod schemas + API-error→form mapping shared by auth forms
docs/                           # requirements, schema, plan, architecture
```
