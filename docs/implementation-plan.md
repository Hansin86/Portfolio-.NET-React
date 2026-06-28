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
| **Application** | ✅ Pipeline wired (`AddApplication()`: MediatR, AutoMapper, FluentValidation + `ValidationBehaviour`). Auth ports defined. Auth features landed: `Register`/`Login` commands + handlers + validators + `AuthResponseDto` |
| **API** | ✅ `AuthController` (`register`/`login`), JWT bearer auth + `UseAuthentication()`, global `ExceptionHandlingMiddleware`. WeatherForecast sample removed |

Local dev infra: `docker-compose.yml` runs Postgres (`portfolio-db`, port 5432) +
pgAdmin (`portfolio-pgadmin`, http://localhost:5050). Credentials come from a gitignored
`.env` (`.env.example` is the committed template). The API reads its connection string
from **user-secrets** (`ConnectionStrings:Default`). `dotnet-ef` is installed as a local
tool (`dotnet-tools.json` manifest) — restore with `dotnet tool restore`.

---

## ✅ Done: Authentication vertical slice (FR-01, NFR-03; foundation for FR-03, FR-02, NFR-04)

Auth is the hard dependency for everything else: FR-03 (users see only their own data)
and NFR-04 (all endpoints JWT-protected) gate every other feature. This slice also wires
the entire pipeline once, so later features just repeat the pattern. **All five steps below
are complete; full test suite (24 unit + 5 integration) is green.**

Requirement status — what this slice actually delivered:
- **FR-01** (register) ✅ and **NFR-03** (bcrypt-hashed passwords) ✅ — fully satisfied.
- **FR-02** (log in / log out) — login ✅; logout not handled (stateless JWT, no
  revocation yet).
- **FR-03** (per-user data isolation) — **foundation only.** JWT carries the user id
  (`sub`), but no data endpoints / `[Authorize]` / per-user scoping exist yet; realized
  when Transactions CRUD lands.
- **NFR-04** (all endpoints JWT-protected) — **foundation only.** Authentication +
  `UseAuthorization()` are wired, but no endpoint carries `[Authorize]` (the only
  endpoints, `register`/`login`, are intentionally anonymous). Enforcement arrives with
  the first protected feature.

Next up is the CI pipeline (recommended) then Transactions CRUD — see "Feature sequence
after auth" below.

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
3. ✅ **Use cases** — `Features/Auth/Register/` and `Features/Auth/Login/`:
   `RegisterCommand`/`LoginCommand` (records) + handler + validator each, returning a
   shared `AuthResponseDto` (`UserId`, `Email`, JWT). Handlers normalize email
   (trim + lowercase), hash/verify via `IPasswordHasher`, and issue a token via
   `IJwtTokenGenerator`. New `Domain/Exceptions`: `EmailAlreadyInUseException`,
   `InvalidCredentialsException`. Register password policy: 12–128 chars, upper/lower/digit,
   no whitespace.
4. ✅ **API** — thin `AuthController` (`POST /auth/register`, `POST /auth/login`) over
   MediatR `ISender`; JWT bearer auth wired in `Program.cs` (`TokenValidationParameters`
   from `JwtSettings`) + `app.UseAuthentication()`; global `ExceptionHandlingMiddleware`
   maps `ValidationException`→400, `EmailAlreadyInUseException`→409,
   `InvalidCredentialsException`→401, else 500 (RFC 7807). `Jwt` issuer/audience in
   `appsettings.json`, secret `Jwt:Key` in user-secrets. **WeatherForecast sample deleted.**
5. ✅ **Tests** — 24 unit tests (register/login handlers + both validators); 5 integration
   tests via `WebApplicationFactory<Program>` + Testcontainers Postgres hitting
   `/auth/register` → `/auth/login` (plus duplicate-email 409, weak-password 400,
   bad-credential 401). Full suite green.

---

## Next slice: Transactions CRUD (FR-05, FR-06, FR-07)

First per-user feature. It establishes the ownership/scoping pattern (**FR-03**) and the
first `[Authorize]`-protected endpoints (**NFR-04** enforcement), so later features just
repeat it. The `Transactions`, `Assets`, and `Portfolios` tables already exist from the
`InitialCreate` migration — **no schema migration is needed**; this slice is Application +
API + repository code, plus two small prerequisites.

### Prerequisites (cross-cutting — do these first)

- **P1 — Current-user accessor.** Add an `ICurrentUserService` port in Application exposing
  `UserId` (Guid) and, for later, `IsDemo` / `DemoSessionId`. Implement over
  `IHttpContextAccessor` (read the `sub` claim); register `AddHttpContextAccessor()` + the
  service in the composition root. This is the read-side of FR-03 and the basis for the
  first enforced endpoints.
- **P2 — Portfolio bootstrap.** Registration currently creates only a `User`, so the
  one-portfolio-per-user invariant isn't established yet. Extend `RegisterCommandHandler`
  to also create the user's `Portfolio` (default `BaseCurrency`, e.g. `"USD"` — settable
  later via FR-11) in the same unit of work. Add `IPortfolioRepository`
  (`GetByUserIdAsync`). _(Alternative: lazily ensure-portfolio inside the add-transaction
  handler; creating it at registration is cleaner and keeps the invariant explicit —
  recommended.)_
- **P3 — `NotFoundException`** in `Domain/Exceptions`, mapped to **404** in
  `ExceptionHandlingMiddleware`. Used when a transaction is missing _or not owned_ by the
  caller — return 404 (not 403) so existence isn't leaked.

### Steps

1. **Domain.** No entity changes expected — `Transaction`, `Asset`, `Portfolio` and their
   EF configs already exist. Optional: a small domain helper to compute current held
   quantity / weighted-average cost if you want the over-sell guard in step 3. No migration.
2. **Ports (Application interfaces).**
   - `ITransactionRepository` — `AddAsync`, `GetByIdAsync`, `Update`, `Remove`, and a
     portfolio-scoped list method taking filter/sort parameters.
   - `IAssetRepository` — `GetByTickerAsync`, `AddAsync` (for get-or-create).
   - `IPortfolioRepository` (from P2).
   - All registered scoped in `AddInfrastructure()`.
3. **Use cases — `Application/Features/Transactions/`** (one folder per operation, CQRS):
   - **`AddTransaction/`** — `AddTransactionCommand(Ticker, Type, Quantity, PricePerUnit,
     Currency, TransactionDate, AssetType?)` → `TransactionDto`. Handler: resolve the
     caller's portfolio (`ICurrentUserService` + `IPortfolioRepository`); **get-or-create**
     the `Asset` by ticker (interim: `Name = Ticker`, `AssetType` from request or default
     `Stock`, `QuoteCurrency = Currency` — real metadata arrives with FR-08); create +
     persist the `Transaction`. Optional rule: reject a `Sell` exceeding current holdings →
     `DomainException` (422/400).
   - **`EditTransaction/`** — `EditTransactionCommand(Id, …editable fields)`. Load by id,
     verify it belongs to the caller's portfolio (else `NotFoundException`), apply, persist.
   - **`DeleteTransaction/`** — `DeleteTransactionCommand(Id)`. Load + ownership check, remove.
   - **`GetTransactions/`** — `GetTransactionsQuery(AssetTicker?, Type?, FromDate?, ToDate?,
     SortBy?, Descending?, Page?, PageSize?)` → list of `TransactionDto` (FR-07 sort/filter),
     scoped to the caller's portfolio.
   - **(optional) `GetTransactionById/`** to back a `GET /{id}` endpoint.
   - **Validators** next to each command: `Quantity > 0`; `PricePerUnit > 0`; `Currency`
     ISO-4217 (3 uppercase letters); `Ticker` non-empty, max length 20; `TransactionDate`
     not in the future; `Type` defined.
   - **`TransactionDto`** (`Id`, `Ticker`, `AssetName`, `Type`, `Quantity`, `PricePerUnit`,
     `Currency`, `TransactionDate`) + an **AutoMapper profile** in `Common/Mappings/`.
4. **Infrastructure.** Implement the three repositories over `PortfolioDbContext`; implement
   `ICurrentUserService` (here or in API). Lean on the existing
   `IX_Transactions_PortfolioId_AssetId` and `IX_Transactions_TransactionDate` indexes for
   the filtered/sorted list query. Register everything in `AddInfrastructure()`.
5. **API — `TransactionsController`** (`[Authorize]`, `[Route("transactions")]`, thin over
   `ISender`):
   - `POST /transactions` → **201 Created** (`AddTransactionCommand`)
   - `GET /transactions` → **200** (query params → `GetTransactionsQuery`)
   - `GET /transactions/{id}` → **200 / 404** (optional)
   - `PUT /transactions/{id}` → **200/204** (`EditTransactionCommand`, id from route)
   - `DELETE /transactions/{id}` → **204 / 404**

   XML summaries + `ProducesResponseType` on every action. Wire `AddHttpContextAccessor()`
   and the current-user service in the composition root.
6. **Tests.**
   - **Unit** (`tests/.../Features/Transactions/`): add-handler (get-or-create asset,
     portfolio resolution, optional over-sell guard), edit/delete ownership checks
     (`NotFoundException` for another user's id), list filtering/sorting, and all validators.
   - **Integration:** extend the harness with an authenticated-client helper (register →
     login → attach bearer token); exercise the full CRUD lifecycle; assert **FR-03**
     isolation (user A cannot read/edit/delete user B's transaction → 404) and that
     `[Authorize]` returns **401** without a token (**NFR-04**).

**Requirements this slice moves:** FR-05/06/07 → implemented; FR-03 → first real
enforcement (per-portfolio scoping + ownership); NFR-04 → first `[Authorize]` endpoints.

---

## Next slice (frontend): Frontend foundation + auth & transactions UI

Stand up the React/TypeScript app once the auth and Transactions CRUD endpoints exist, so
there's a working **end-to-end vertical slice** (register/login → manage transactions)
before more backend features land. This also gets the frontend into the repo (**NFR-06**)
and establishes the patterns (API client, auth/token handling, routing, forms) that every
later UI screen reuses. The dashboard and chart screens stay deferred until their backends
(FR-13–FR-21) exist — see the revised sequence below.

### Prerequisites (cross-cutting — do these first)

- **F1 — CORS.** Add an ASP.NET Core CORS policy allowing the frontend dev origin
  (Vite default `http://localhost:5173`); read allowed origins from config so prod can
  differ.
- **F2 — Auth contract check.** Confirm `AuthResponseDto` (JWT + `UserId`/`Email`) is what
  the client needs to bootstrap a session; no token-refresh endpoint exists (stateless
  JWT), so the client treats expiry as "log in again."

### Steps

1. **Scaffold.** Vite + React + TypeScript app under `frontend/` (or `src/web/`). Add
   ESLint/Prettier, an `.env` for the API base URL, and an npm scripts baseline
   (`dev`/`build`/`lint`/`test`). Commit a README note on running it alongside the API.
2. **API client + auth.** Typed `fetch`/axios client with a base URL and a request
   interceptor that attaches the `Authorization: Bearer <jwt>` header. Auth context/store
   holding the token + user; persist to `localStorage`; redirect to login on `401`.
3. **Routing + layout.** React Router with public routes (`/login`, `/register`) and a
   protected-route wrapper guarding the app shell. Minimal layout (nav + sign-out).
4. **Auth screens (FR-01, FR-02).** Register and login forms with client-side validation
   mirroring the backend password policy; surface API errors (409 duplicate email, 401 bad
   credentials, 400 validation). Sign-out clears the stored token.
5. **Transactions UI (FR-05, FR-06, FR-07).** Transactions list with sort + filter
   (by asset/date) backed by `GET /transactions`; add/edit transaction form
   (`POST`/`PUT`); delete with confirm. Loading/empty/error states.
6. **Tests + CI.** Component/integration tests (Vitest + React Testing Library); extend the
   GitHub Actions workflow (NFR-07) with a frontend job (install → lint → build → test).

**Requirements this slice moves:** FR-01/FR-02 → usable UI (incl. logout in the client);
FR-05/06/07 → usable UI; NFR-06 → frontend in the repo; NFR-07 → CI covers the frontend.

---

## Feature sequence after auth

1. **Transactions CRUD** (FR-05, FR-06, FR-07) — _detailed steps above._ First real
   per-user feature; exercises the auth/ownership pattern.
2. **Frontend foundation + auth & transactions UI** (FR-01, FR-02, FR-05–FR-07; NFR-06) —
   _detailed steps above._ React/TS app, API client + auth, and the transactions UI —
   first working end-to-end slice. Later UI screens (dashboard, charts) layer on as their
   backends land.
3. **Market data + FX** (FR-08–FR-12) — Alpha Vantage client behind an Application
   interface, Infrastructure implementation; Hangfire job for scheduled price refresh;
   multi-currency asset support; FX rate fetching.
4. **Portfolio summary** (FR-13–FR-17) — weighted-average cost basis, current value,
   P&L (absolute + %), per-asset rows, all converted to base currency at display time.
   _(+ dashboard UI on the frontend foundation.)_
5. **Demo session** (FR-04) — seed read-only `demo_portfolio_template` at startup; copy
   per login with `demo_session_id`; demo JWT claims `is_demo` + `demo_session_id`;
   Hangfire cleanup job for sessions older than 60 min. _(+ "Try the demo" entry on the UI.)_
6. **Charts endpoints + UI** (FR-18–FR-21) — value-over-time, allocation, per-asset price
   history, P&L history; Recharts visuals on the frontend.
7. **Deployment** — Docker Compose for full stack (NFR-05); Railway (backend+DB) +
   Vercel (frontend).

## Cross-cutting (slot in early)

- ✅ **CI pipeline (NFR-07)** — GitHub Actions workflow added (`.github/workflows/ci.yml`):
  build + unit + integration tests on push / PRs to main. On branch
  `ci/github-actions-pipeline`, active once merged.
- **OpenAPI/Scalar (NFR-01)** — already scaffolded; keep endpoints documented as they land.

---

## Key rules to honor while building

- Vertical slices: Domain → Application (command/query + handler + validator + any new
  interface) → Infrastructure impl → API endpoint.
- CQRS via MediatR; thin controllers; no business logic in controllers or EF configs.
- P&L uses **weighted average cost** (not FIFO). Monetary values stored in original
  transaction currency; converted at display time using stored FX rates.
