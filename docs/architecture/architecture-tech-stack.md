# Architecture — Tech Stack

Part of the architecture reference (see `architecture-layers.md` for the layering). This
document lists the frameworks, libraries, and tooling in use, grouped by where they live.
Versions are taken from the `.csproj` files as of this writing — treat the project files
as the source of truth if they drift.

> **Note:** the target framework across every project is **.NET 10** (`net10.0`), and EF
> Core / Npgsql are on the **10.x** line (the `CLAUDE.md` summary's "EF Core 9" predates
> the upgrade — the csproj shows `10.0.9`).

---

## Runtime & platform

| Component | Version | Notes |
|-----------|---------|-------|
| .NET / target framework | `net10.0` | all projects |
| ASP.NET Core | 10 | Web API host (`PortfolioApp.API`) |
| PostgreSQL | (Docker) | primary datastore + Hangfire storage |
| Solution format | `.slnx` | newer XML solution file (`PortfolioApp.slnx`) |

---

## Backend libraries by layer

### Domain (`PortfolioApp.Domain`)
No external packages — by design. Pure C# entities, enums, and exceptions.

### Application (`PortfolioApp.Application`)
| Package | Version | Purpose |
|---------|---------|---------|
| MediatR | 14.1.0 | CQRS request/handler dispatch |
| FluentValidation.DependencyInjectionExtensions | 12.1.1 | input validation (pipeline behavior) |
| AutoMapper | 16.1.1 | entity ↔ DTO mapping |
| Microsoft.Extensions.Logging.Abstractions | 10.0.9 | logging abstractions without a host dependency |

### Infrastructure (`PortfolioApp.Infrastructure`)
| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore | 10.0.9 | ORM |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.2 | PostgreSQL provider |
| Microsoft.EntityFrameworkCore.Design | 10.0.9 | design-time tools (migrations) |
| Hangfire.AspNetCore | 1.x | background/scheduled jobs |
| Hangfire.PostgreSql | 1.x | Hangfire storage in PostgreSQL |
| AutoMapper | 16.1.1 | mapping (shared) |

### API (`PortfolioApp.API`)
| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.9 | JWT bearer auth |
| Microsoft.AspNetCore.OpenApi | 10.0.9 | built-in OpenAPI document |
| Scalar.AspNetCore | 2.16.4 | interactive API docs at `/scalar/v1` |
| Serilog.AspNetCore | 10.0.0 | structured logging |
| Swashbuckle.AspNetCore | 10.2.2 | OpenAPI/Swagger tooling |
| Newtonsoft.Json | 13.0.3 | JSON serialization support |
| AutoMapper | 16.1.1 | mapping (shared) |

---

## Testing

| Package | Version | Used in | Purpose |
|---------|---------|---------|---------|
| xunit | 2.9.3 | both | test framework |
| xunit.runner.visualstudio | 3.1.5 | both | test runner |
| Microsoft.NET.Test.Sdk | 18.6.0 | both | test SDK/host |
| FluentAssertions | 8.10.0 | both | assertion syntax |
| NSubstitute | 5.x | both | mocking |
| coverlet.collector | 10.0.1 | both | code coverage |
| AutoFixture | 4.x | unit | test data generation |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.9 | integration | `WebApplicationFactory` host |
| Testcontainers.PostgreSql | 4.12.0 | integration | disposable PostgreSQL (needs Docker) |

- **`tests/PortfolioApp.UnitTests`** — references Application + Domain; pure unit tests with
  mocked ports.
- **`tests/PortfolioApp.IntegrationTests`** — references API + Infrastructure; drives the
  real API via `WebApplicationFactory` against a Testcontainers PostgreSQL instance
  (Docker must be running). Each factory injects its per-host config (container connection
  string, JWT settings) through `ConfigureTestServices` rather than process-global
  environment variables, so factories share no process state and test collections run in
  parallel safely. JWT validation is overridden via `JwtSettings` (not `JwtBearerOptions`)
  because `Program.cs` builds the bearer parameters from those options.

---

## Tooling & local dev

| Tool | Purpose |
|------|---------|
| `dotnet-ef` (local tool) | EF Core migrations; restore via `dotnet tool restore` (`dotnet-tools.json`) |
| Docker Compose | local PostgreSQL (`portfolio-db`, 5432) + pgAdmin (`portfolio-pgadmin`, :5050) |
| user-secrets | local connection string (`ConnectionStrings:Default`) |
| `.env` / `.env.example` | gitignored DB credentials for Compose; committed template |

---

## Frontend *(planned)*

Not yet in the repo. Target stack per `CLAUDE.md` / requirements:

| Component | Purpose |
|-----------|---------|
| React + TypeScript | SPA frontend |
| Recharts | portfolio charts (value over time, allocation, P&L history) |

---

## External services

| Service | Purpose |
|---------|---------|
| Alpha Vantage API | market prices (FR-08) + FX rates (FR-12) — see `architecture.md` |
| PostgreSQL | persistence + Hangfire job storage |

---

## Deployment *(planned)*

| Target | Role |
|--------|------|
| Docker Compose | full-stack local/prod compose (NFR-05) |
| Railway | backend + database hosting |
| Vercel | frontend hosting |
| GitHub Actions | CI: build + unit tests on every push (NFR-07) |
