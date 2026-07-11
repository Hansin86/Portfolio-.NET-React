# Implementation Plan

Working roadmap for building out the portfolio tracker. Pairs with `requirements.md`
(the *what*) and `database-schema.md` (the data model). This file is the *how / in what
order*. Update it as commits and slices land (see `.claude/skills/update-plan`).

**Markers:** slices use ✅ done · 🚧 in progress · ⬜ not started. Commits use
`- [x]` done · `- [ ]` not done. "Key decisions" on each slice captures deviations worth
remembering; deeper rationale lives in git history and the code.

---

## Current State (2026-07-11)

| Layer | State |
|-------|-------|
| **Frontend** | 🚧 In progress (2 of 9 commits). Vite 8 + React 19 + TS under `frontend/`, oxlint + Prettier + Vitest, `VITE_API_BASE_URL`, dev server :5173. Typed API client landed (`src/api/`: `types`/`requests`/`client`/`errors` + `auth`/`transactions` fns, axios interceptors for bearer + global 401). No UI yet. |
| **Domain** | ✅ 7 entities + enums, `Currency` value object (`ValueObjects/Currency.cs` + `Iso4217`), exceptions `EmailAlreadyInUse`/`InvalidCredentials`/`NotFoundException`/`DomainException`. |
| **Infrastructure** | ✅ `PortfolioDbContext` + EF configs + DI + initial migration. `CurrencyConverter` via `ConfigureConventions` (varchar(3), no migration). Ports impl: `PasswordHasher` (bcrypt), `JwtTokenGenerator`, `UserRepository`, `PortfolioRepository`, `TransactionRepository` (+ `GetHeldQuantityAsync`), `AssetRepository` (get-or-create). |
| **Application** | ✅ Pipeline wired (MediatR, AutoMapper, FluentValidation + `ValidationBehaviour`). Ports: auth + `ICurrentUserService`/`IPortfolioRepository`/`ITransactionRepository`/`IAssetRepository`. Auth (`Register`/`Login`, register bootstraps `Portfolio` @ USD) + Transactions CRUD landed. `TransactionDto`, `PagedResult<T>`, `TransactionProfile`. |
| **API** | ✅ `AuthController` (`register`/`login`) + `TransactionsController` (`[Authorize]` CRUD). JWT bearer + `ExceptionHandlingMiddleware` (404/422/400/401/409), `CurrentUserService`, `JsonStringEnumConverter`. `SpaCors` policy wired. |

Local dev infra: `docker-compose.yml` runs Postgres (`portfolio-db`, :5432) + pgAdmin
(:5050); credentials from gitignored `.env` (`.env.example` is the template). API reads
its connection string from user-secrets. `dotnet-ef` is a local tool (`dotnet tool restore`).

---

## Slices

### ✅ Authentication (FR-01, FR-02 login, NFR-03)

Hard dependency for everything else — wired the whole MediatR/validation/mapping pipeline
once so later slices repeat the pattern. Full suite green (24 unit + 5 integration).

Commits:
- [x] Application wiring (`AddApplication()`: MediatR + AutoMapper + FluentValidation + `ValidationBehaviour`)
- [x] Ports + impls (`IPasswordHasher`/`IJwtTokenGenerator`/`IUserRepository` → bcrypt hasher, JWT generator, EF repo)
- [x] Use cases (`Register`/`Login` commands + handlers + validators, `AuthResponseDto`)
- [x] API (`AuthController`, JWT bearer in `Program.cs`, `ExceptionHandlingMiddleware`; WeatherForecast removed)
- [x] Tests (24 unit + 5 integration via `WebApplicationFactory` + Testcontainers)

Key decisions: stateless JWT, no logout/refresh (expiry ⇒ re-login); password policy
12–128 chars, upper/lower/digit, no whitespace; `sub` carries user id. FR-03/NFR-04 were
foundation-only here — first real enforcement arrived with Transactions CRUD.

### ✅ Transactions CRUD (FR-05, FR-06, FR-07)

First per-user feature — establishes ownership/scoping (**FR-03**) and the first
`[Authorize]` endpoints (**NFR-04**). No schema migration (tables existed from
`InitialCreate`). Full suite green (98 unit + 9 integration).

Prerequisites:
- [x] P1 — `ICurrentUserService` (reads `sub` claim, `NameIdentifier` fallback) + impl over `IHttpContextAccessor`
- [x] P2 — Portfolio bootstrap in `RegisterCommandHandler` (same unit of work) + `IPortfolioRepository`
- [x] P3 — `NotFoundException` → 404 (used for missing *or* not-owned, so existence isn't leaked)
- [x] Bonus — `Currency` value object (`Currency.From` validates against `Iso4217`; varchar(3), no migration)

Commits:
- [x] Ports + repos (`ITransactionRepository` + `GetHeldQuantityAsync`, `IAssetRepository` get-or-create)
- [x] `AddTransaction` command + validator (get-or-create asset, resolve caller portfolio)
- [x] `EditTransaction` + `DeleteTransaction` (ownership check → 404) with over-sell guard
- [x] `GetTransactions` / `GetTransactionById` (filter/sort/page, `PagedResult<T>`)
- [x] `TransactionsController` (`[Authorize]`) + `TransactionProfile` + tests

Key decisions: over-sell guard on add/edit/delete (net held qty ≥ 0, else 422) via
DB-side `GetHeldQuantityAsync`; paged list default 20 / cap 100, default sort
`TransactionDate` desc with `.ThenBy(Id)` tie-breaker; ticker/asset not editable (delete +
re-add); enums serialize as names; POST 201+Location, PUT 200, DELETE 204; asset
get-or-create seeds interim metadata until FR-08.

### 🚧 Frontend foundation + auth & transactions UI (FR-01, FR-02, FR-05–FR-07; NFR-06, NFR-07)

Stand up the React/TS app for a working end-to-end slice (register/login → manage
transactions) and establish the patterns (API client, auth/token, routing, forms) every
later screen reuses. Dashboard/charts stay deferred until their backends exist.

Commits:
- [x] Scaffold + tooling (Vite 8 + React 19 + TS under `frontend/`, oxlint + Prettier + Vitest, `.env`, dev server :5173)
- [x] Types + API client (`src/api/`: `types`/`requests`/`client`/`errors` + `auth`/`transactions`; axios bearer + 401 interceptors via `setAuthToken`/`setUnauthorizedHandler`)
- [ ] Auth context + session (`AuthProvider`, token+user in `localStorage`, 401 → clear + redirect; wire `QueryClientProvider`)
- [ ] Routing + layout (React Router, public `/login`+`/register`, `RequireAuth` shell)
- [ ] Auth screens (RHF + Zod mirroring password policy; map `400.errors` to fields, `409`/`401` form-level)
- [ ] Transactions list (`useTransactions` query, filter/sort/page, loading/empty/error states)
- [ ] Transactions create/edit/delete (Zod-validated form → POST/PUT, confirm-delete → DELETE, invalidate list, `422` form-level)
- [ ] Component/integration tests (Vitest + RTL + MSW)
- [ ] CI frontend job (`.github/workflows/ci.yml`, path-filtered `frontend/**`)

Stack decisions: axios instance + interceptors · TanStack Query (server state) · React
Context + `localStorage` (session) · React Hook Form + Zod (forms) · React Router · CSS
Modules · Vitest + RTL + MSW. API base URL is the only host reference (read once from
`import.meta.env.VITE_API_BASE_URL`, inlined at build time). Bearer-in-header (not cookies)
keeps cross-origin Vercel↔Railway friction-free; add `vercel.json` SPA rewrite when
deploying. F1 CORS prereq (`SpaCors`) is ✅ done and integration-tested.

API contract the client codes against: `POST /auth/register|login` → `AuthResponseDto
{ userId, email, token }`. Transactions (all `Bearer`): `POST` 201+Location, `GET` list →
`PagedResult<TransactionDto>` (params `assetTicker/type/fromDate/toDate/sortBy/descending/
page/pageSize`), `GET /{id}` 200/404, `PUT /{id}` 200, `DELETE /{id}` 204. Enums are
strings; errors are RFC 7807 (`400` field errors map, `401`, `404` also for others'
resources, `409` dup email, `422` over-sell); dates ISO `YYYY-MM-DD`; money in original
currency (no base conversion until FR-13–FR-17).

### ⬜ Market data + FX (FR-08–FR-12)

Alpha Vantage client behind an Application interface, Infrastructure impl. Real asset
metadata (replaces the interim seed from the CRUD slice), multi-currency support, FX rates.

Commits:
- [ ] `IMarketDataClient` / `IFxRateClient` ports + Alpha Vantage impls
- [ ] `PriceSnapshot` / `FxRate` persistence + repositories
- [ ] Hangfire scheduled price-refresh job
- [ ] Asset metadata enrichment (replace interim get-or-create seed)
- [ ] Tests

### ⬜ Portfolio summary (FR-13–FR-17)

Weighted-average cost basis, current value, P&L (absolute + %), per-asset rows, converted
to base currency at display time using stored FX rates. + dashboard UI on the FE foundation.

Commits:
- [ ] Cost-basis / value / P&L query + handler (weighted-average cost)
- [ ] Base-currency conversion at display time
- [ ] `SummaryController` endpoint + DTOs
- [ ] Dashboard UI (frontend)
- [ ] Tests

### ⬜ Demo session (FR-04)

Isolated, auto-expiring demo portfolio.

Commits:
- [ ] Seed read-only `demo_portfolio_template` at startup
- [ ] Per-login copy with `demo_session_id`; demo JWT claims `is_demo` + `demo_session_id`
- [ ] Hangfire cleanup job (sessions older than 60 min)
- [ ] "Try the demo" entry (frontend)
- [ ] Tests

### ⬜ Charts endpoints + UI (FR-18–FR-21)

Value-over-time, allocation, per-asset price history, P&L history; Recharts on the FE.

Commits:
- [ ] Chart data endpoints (value-over-time, allocation, price history, P&L history)
- [ ] Recharts visuals (frontend)
- [ ] Tests

### ⬜ Deployment (NFR-05)

Commits:
- [ ] Docker Compose for full stack
- [ ] Railway (backend + DB)
- [ ] Vercel (frontend) + `vercel.json` SPA rewrite + prod CORS origins

---

## Cross-cutting

- ✅ **CI pipeline (NFR-07)** — `.github/workflows/ci.yml`: build + unit + integration on push/PR to main.
- ⬜ **OpenAPI/Scalar (NFR-01)** — scaffolded; keep endpoints documented as they land.
- ⬜ **Align EF Core package versions** — `MSB3277` conflict (Relational 10.0.4 vs 10.0.9); harmless, pin one version to clear.

---

## Key rules to honor while building

- Vertical slices: Domain → Application (command/query + handler + validator + any new
  interface) → Infrastructure impl → API endpoint.
- CQRS via MediatR; thin controllers; no business logic in controllers or EF configs.
- P&L uses **weighted average cost** (not FIFO). Monetary values stored in original
  transaction currency; converted at display time using stored FX rates.
