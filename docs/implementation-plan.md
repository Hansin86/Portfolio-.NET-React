# Implementation Plan

Working roadmap for building out the portfolio tracker. Pairs with
`requirements.md` (the *what*) and `database-schema.md` (the data model). This file is
the *how / in what order*. Update it as slices land.

---

## Current state (2026-06-24)

| Layer | State |
|-------|-------|
| **Domain** | ✅ All 7 entities (`User`, `Portfolio`, `DemoSession`, `Asset`, `Transaction`, `PriceSnapshot`, `FxRate`) + enums |
| **Infrastructure** | ✅ `PortfolioDbContext`, EF configurations, DI registration + initial migration. Auth ports implemented: bcrypt `PasswordHasher`, `JwtTokenGenerator` (+ `JwtSettings`), `UserRepository`, all wired in `AddInfrastructure()` |
| **Application** | ⚠️ Pipeline wired (`AddApplication()`: MediatR, AutoMapper, FluentValidation + `ValidationBehaviour`). Auth ports defined (`IPasswordHasher`, `IJwtTokenGenerator`, `IUserRepository`). No features/validators/mappings yet |
| **API** | ⚠️ Only the `WeatherForecast` sample — no auth, no MediatR wiring, no exception middleware |

Local dev infra: `docker-compose.yml` runs Postgres (`portfolio-db`, port 5432) +
pgAdmin (`portfolio-pgadmin`, http://localhost:5050). Credentials come from a gitignored
`.env` (`.env.example` is the committed template). The API reads its connection string
from **user-secrets** (`ConnectionStrings:Default`). `dotnet-ef` is installed as a local
tool (`dotnet-tools.json` manifest) — restore with `dotnet tool restore`.

---

## Next step: Authentication vertical slice (FR-01, FR-02, FR-03, NFR-03, NFR-04)

Auth is the hard dependency for everything else: FR-03 (users see only their own data)
and NFR-04 (all endpoints JWT-protected) gate every other feature. This slice also wires
the entire pipeline once, so later features just repeat the pattern.

1. ✅ **Application wiring** — `DependencyInjection.AddApplication()` registers MediatR,
   AutoMapper, and FluentValidation validators from the Application assembly, plus the
   `ValidationBehaviour<,>` MediatR pipeline behavior (registered as an open behavior).
   Packages were already referenced; project builds clean. Still to wire into the API
   composition root (step 4).
2. ✅ **Ports** — `IPasswordHasher`, `IJwtTokenGenerator`, and `IUserRepository`
   interfaces in Application; Infrastructure implementations: bcrypt `PasswordHasher`
   (`BCrypt.Net-Next`), `JwtTokenGenerator` using `JsonWebTokenHandler`/HMAC-SHA256 with
   `JwtSettings` options, and EF Core `UserRepository`. Registered in `AddInfrastructure()`
   (hasher + token generator as singletons, repository scoped). Builds clean.
   **Remaining:** populate the `Jwt` config section (key/issuer/audience) via user-secrets
   in step 4.
3. **Use cases** — `RegisterCommand` + handler + validator; `LoginCommand`/`Query` +
   handler returning a JWT.
4. **API** — thin `AuthController` (`register` / `login`); add JWT bearer auth +
   `app.UseAuthentication()`; global exception-handling middleware; register Application
   and new Infrastructure services; **delete the WeatherForecast sample**.
5. **Tests** — unit test for the register handler; integration test hitting
   `/auth/register` then `/auth/login` (Testcontainers — Docker must be running).

---

## Feature sequence after auth

1. **Transactions CRUD** (FR-05, FR-06, FR-07) — buy/sell, edit/delete, history with
   sort/filter. First real per-user feature; exercises the auth/ownership pattern.
2. **Market data + FX** (FR-08–FR-12) — Alpha Vantage client behind an Application
   interface, Infrastructure implementation; Hangfire job for scheduled price refresh;
   multi-currency asset support; FX rate fetching.
3. **Portfolio summary** (FR-13–FR-17) — weighted-average cost basis, current value,
   P&L (absolute + %), per-asset rows, all converted to base currency at display time.
4. **Demo session** (FR-04) — seed read-only `demo_portfolio_template` at startup; copy
   per login with `demo_session_id`; demo JWT claims `is_demo` + `demo_session_id`;
   Hangfire cleanup job for sessions older than 60 min.
5. **Charts endpoints** (FR-18–FR-21) — value-over-time, allocation, per-asset price
   history, P&L history.
6. **React + TypeScript frontend** — dashboard, transaction management, Recharts visuals.
7. **Deployment** — Docker Compose for full stack (NFR-05); Railway (backend+DB) +
   Vercel (frontend).

## Cross-cutting (slot in early)

- **CI pipeline (NFR-07)** — GitHub Actions: build + unit tests on every push. Cheap to
  add now and protects every later slice. Recommend doing this right after the auth slice.
- **OpenAPI/Scalar (NFR-01)** — already scaffolded; keep endpoints documented as they land.

---

## Key rules to honor while building

- Vertical slices: Domain → Application (command/query + handler + validator + any new
  interface) → Infrastructure impl → API endpoint.
- CQRS via MediatR; thin controllers; no business logic in controllers or EF configs.
- P&L uses **weighted average cost** (not FIFO). Monetary values stored in original
  transaction currency; converted at display time using stored FX rates.
