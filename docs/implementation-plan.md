# Implementation Plan

Working roadmap for building out the portfolio tracker. Pairs with
`requirements.md` (the *what*) and `database-schema.md` (the data model). This file is
the *how / in what order*. Update it as slices land.

---

## Current state (2026-06-28)

| Layer | State |
|-------|-------|
| **Domain** | ✅ All 7 entities (`User`, `Portfolio`, `DemoSession`, `Asset`, `Transaction`, `PriceSnapshot`, `FxRate`) + enums. **`Currency` value object** (`ValueObjects/Currency.cs` + `Iso4217` code set) replaces the raw currency strings on all entities. Exceptions: `EmailAlreadyInUse`, `InvalidCredentials`, **`NotFoundException`**, **`DomainException`** |
| **Infrastructure** | ✅ `PortfolioDbContext`, EF configurations, DI registration + initial migration. **`CurrencyConverter`** wired via `ConfigureConventions` (keeps `varchar(3)` — no migration). Auth ports implemented: bcrypt `PasswordHasher`, `JwtTokenGenerator` (+ `JwtSettings`), `UserRepository`, **`PortfolioRepository`**. **`TransactionRepository`** (filter/sort/page list + `GetHeldQuantityAsync`) + **`AssetRepository`** (get-or-create by ticker). All wired in `AddInfrastructure()` |
| **Application** | ✅ Pipeline wired (`AddApplication()`: MediatR, AutoMapper, FluentValidation + `ValidationBehaviour`). Auth ports defined + **`ICurrentUserService`**, **`IPortfolioRepository`**, **`ITransactionRepository`**, **`IAssetRepository`**. Auth features landed (`Register`/`Login`); registration **bootstraps the user's `Portfolio`** (default base currency `USD`). **Transactions CRUD** landed: `AddTransaction`/`EditTransaction`/`DeleteTransaction` commands + `GetTransactions`/`GetTransactionById` queries (+ validators), `TransactionDto`, `PagedResult<T>`, first AutoMapper profile (`Common/Mappings/TransactionProfile`) |
| **API** | ✅ `AuthController` (`register`/`login`) + **`TransactionsController`** (`[Authorize]` CRUD — first JWT-protected endpoints, NFR-04). JWT bearer auth + `UseAuthentication()`, global `ExceptionHandlingMiddleware` (maps `NotFoundException`→404, `DomainException`→422, `ValidationException`→400). **`CurrentUserService`** (over `IHttpContextAccessor`) wired. **`JsonStringEnumConverter`** registered so enums serialize as names. WeatherForecast sample removed |

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

## ✅ Done: Transactions CRUD (FR-05, FR-06, FR-07)

First per-user feature. It establishes the ownership/scoping pattern (**FR-03**) and the
first `[Authorize]`-protected endpoints (**NFR-04** enforcement), so later features just
repeat it. No schema migration was needed (`Transactions`/`Assets`/`Portfolios` already
existed from `InitialCreate`). **Landed in 8 small commits; full suite green: 98 unit + 9
integration tests.**

Requirement status — what this slice delivered:
- **FR-05** (add transaction) ✅, **FR-06** (edit/delete) ✅, **FR-07** (filter/sort/page
  list) ✅.
- **FR-03** (per-user isolation) ✅ — first real enforcement: every command/query resolves
  the caller's portfolio via `ICurrentUserService` + `IPortfolioRepository` and scopes to
  it; another user's transaction returns **404** (existence not leaked). Integration-tested.
- **NFR-04** (JWT-protected endpoints) ✅ — `TransactionsController` is the first
  `[Authorize]` controller; unauthenticated requests return **401**. Integration-tested.

Key decisions made while building (deviations from the original plan below):
- **Over-sell guard on Add + Edit + Delete.** Net held quantity per asset (Σ buys − Σ sells)
  must never go negative. Implemented as `ITransactionRepository.GetHeldQuantityAsync`
  (DB-side sum, with an optional `excludeTransactionId` for re-validating edits/deletes)
  rather than a separate domain calculator — simpler to unit-test via the mocked port.
  Violations throw `DomainException` → **422**. (Delete was added to the guard after the
  initial Add+Edit scope, to keep the invariant consistent.)
- **Paged list with total count.** `GetTransactions` returns `PagedResult<TransactionDto>`
  (`Items`, `TotalCount`, `Page`, `PageSize`); default page size 20, **capped at 100**
  (`GetTransactionsQueryValidator.MaxPageSize`). Default sort is `TransactionDate`
  descending. Every sort carries a `.ThenBy(Id)` tie-breaker for deterministic paging.
- **Ticker/asset is not editable.** `EditTransaction` changes trade details only
  (Type/Quantity/PricePerUnit/Currency/Date); correcting a wrong ticker means delete +
  re-add. Keeps the over-sell guard single-asset.
- **`JsonStringEnumConverter` registered globally** so the JSON contract uses enum names
  (`"Buy"`/`"Sell"`, `"Stock"`/`"Etf"`) instead of integers.
- **PUT returns 200** with the updated `TransactionDto`; **POST returns 201** + `Location`
  (`CreatedAtAction` → `GET /{id}`); **DELETE returns 204**.
- **Asset get-or-create** seeds interim metadata (`Name = Ticker`, `QuoteCurrency =`
  transaction currency, `AssetType` from request or `Stock`) — real metadata arrives with
  FR-08.

### Original plan (for reference)

The `Transactions`, `Assets`, and `Portfolios` tables already exist from the
`InitialCreate` migration — **no schema migration is needed**; this slice is Application +
API + repository code, plus two small prerequisites.

### Prerequisites (cross-cutting) — ✅ done

All three landed on `feature/Transactions-CRUD-prerequisites` (build clean; 45 unit tests
green; `dotnet ef migrations has-pending-model-changes` reports none).

- ✅ **P1 — Current-user accessor.** `ICurrentUserService` port (Application) exposing
  `UserId` (Guid), `IsDemo`, `DemoSessionId`. `CurrentUserService` impl (API) over
  `IHttpContextAccessor`, reading the `sub` claim (with `ClaimTypes.NameIdentifier`
  fallback, since the bearer handler maps `sub` by default); throws
  `UnauthorizedAccessException` when absent/malformed. `AddHttpContextAccessor()` + the
  service registered in the composition root. Unit-tested (9 tests; unit-test project now
  references the API project). _Read-side of FR-03._
- ✅ **P2 — Portfolio bootstrap.** `RegisterCommandHandler` now creates the user's
  `Portfolio` (default base currency `Currency.Usd`) attached to the user aggregate, so it
  persists in the **same unit of work** (single `SaveChanges`). `IPortfolioRepository`
  (`GetByUserIdAsync`) + EF `PortfolioRepository` added. Unit-tested.
- ✅ **P3 — `NotFoundException`** in `Domain/Exceptions`, mapped to **404** in
  `ExceptionHandlingMiddleware`. Used when a transaction is missing _or not owned_ by the
  caller — 404 (not 403) so existence isn't leaked. (Integration coverage arrives with the
  CRUD slice, per the project's exception-mapping test convention.)

### Bonus: `Currency` value object (cross-cutting cleanup done alongside the prerequisites)

While bootstrapping the portfolio it was clear the raw `string` currency fields wanted a
single typed source of truth. Added a `Currency` value object (Domain `ValueObjects/`):
sealed record, created only via `Currency.From(code)` which validates against `Iso4217`
codes and throws `DomainException` (→ **422**). All 7 currency properties across the
entities now use `Currency`; persisted as the 3-char code via `CurrencyConverter`, wired
once in `PortfolioDbContext.ConfigureConventions` — **store type unchanged (`varchar(3)`),
so no migration**. Implications for the CRUD slice below:
- The transaction `Currency` validator can use `Iso4217.Codes` / `Currency.From` instead of
  a regex — one source of truth.
- `TransactionDto` should expose currency as the `Code` **string**; the AutoMapper profile
  maps `Currency` → `string` (and back via `Currency.From` where needed).

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

### Stack decisions (proposed defaults — adjust before starting)

| Concern | Choice | Why |
|---------|--------|-----|
| Tooling | **Vite + React + TypeScript**, app under `frontend/` | Fast dev server; keeps the SPA out of the .NET source tree |
| HTTP | **axios** instance with interceptors | Clean place to attach the bearer token and centralize 401 handling |
| Server state | **TanStack Query (React Query)** | Caching, loading/error/empty states, and cache invalidation for the transactions list/mutations — removes hand-rolled fetch boilerplate |
| Auth/session state | **React Context** + `localStorage` | One token + user; no need for Redux at this size |
| Forms + validation | **React Hook Form + Zod** | Zod schemas mirror the backend rules (password policy, transaction fields) as a single client-side source of truth |
| Routing | **React Router** | Public vs. protected route split |
| Styling | **CSS Modules** (minimal) | Keep the first slice lean; a UI kit can come later |
| Tests | **Vitest + React Testing Library + MSW** | Mock the API at the network layer for component/integration tests |

### API contract the client codes against (as built in the Transactions slice)

- **Base URL** from `.env` (`VITE_API_BASE_URL`), e.g. `http://localhost:5029`.
- **Auth:** `POST /auth/register` and `POST /auth/login`, both bodies `{ email, password }`,
  both **200** returning `AuthResponseDto { userId, email, token }` (camelCase JSON). No
  refresh endpoint — treat token expiry / any `401` as "log in again."
- **Transactions** (all require `Authorization: Bearer <jwt>`):
  - `POST /transactions` → **201** + `Location`; body
    `{ ticker, type, quantity, pricePerUnit, currency, transactionDate, assetType? }`.
  - `GET /transactions` → **200** `PagedResult<TransactionDto>`
    `{ items, totalCount, page, pageSize }`; query params `assetTicker?, type?, fromDate?,
    toDate?, sortBy?, descending?, page?, pageSize?` (FR-07).
  - `GET /transactions/{id}` → **200 / 404**.
  - `PUT /transactions/{id}` → **200** (body without `id`: `{ type, quantity, pricePerUnit,
    currency, transactionDate }`).
  - `DELETE /transactions/{id}` → **204**.
- **Enums are strings** (`type` = `"Buy"`/`"Sell"`, `assetType` = `"Stock"`/`"Etf"`,
  `sortBy` = `"TransactionDate" | "Ticker" | "Quantity" | "PricePerUnit" | "Type"`).
- **Errors are RFC 7807 `application/problem+json`:** `400` validation (a
  `ValidationProblemDetails` with an `errors` map keyed by field), `401` unauthorized,
  `404` not found (also returned for another user's resource — FR-03), `409` duplicate email,
  `422` business-rule violations (e.g. over-sell / negative-holding guard). The client should
  map `400.errors` to per-field form errors and surface `409`/`422` `detail` as a form-level
  message.
- `transactionDate` and the date filters are ISO `YYYY-MM-DD` strings; monetary values are
  numbers in the transaction's original `currency` (no base-currency conversion yet —
  that arrives with FR-13–FR-17).

### Prerequisites (cross-cutting — do these first)

- ✅ **F1 — CORS (backend change).** `SpaCors` policy in `Program.cs`: origins read from
  config (`Cors:AllowedOrigins`, defaulting to the Vite dev origin `http://localhost:5173`
  when unset), `AllowAnyHeader` (covers `Authorization`) + `AllowAnyMethod`; no credentials
  (bearer token rides in the header, not cookies). `app.UseCors(...)` wired **before** auth
  middleware. `Cors:AllowedOrigins` documented in `appsettings.json`. Integration-tested
  (`Cors/CorsTests`): preflight `OPTIONS` from the allowed origin is echoed the
  allow-origin header; an unlisted origin is not.
- **F2 — Auth contract check.** ✅ Confirmed: `AuthResponseDto { userId, email, token }` is
  all the client needs to bootstrap a session; stateless JWT, no refresh — expiry ⇒ re-login.

### Steps (small commits)

1. **Scaffold + tooling.** Vite React-TS app under `frontend/`. Add ESLint + Prettier,
   `.env`/`.env.example` (`VITE_API_BASE_URL`), npm scripts (`dev`/`build`/`lint`/`test`),
   and a `frontend/README.md` note on running it alongside the API. `.gitignore` for
   `node_modules`/`dist`. _Commit: empty app boots._
2. **Types + API client.** Hand-written TS types mirroring the contract above
   (`AuthResponse`, `TransactionDto`, `PagedResult<T>`, `TransactionType`, `AssetType`,
   `SortField`, a `ProblemDetails`/`ValidationProblemDetails` shape). An axios instance with
   the base URL, a request interceptor attaching the bearer token, and a response interceptor
   that normalizes problem-details errors and signals `401` for global handling.
   _Commit: typed client, no UI yet._
3. **Auth context + session.** `AuthProvider` (token + user in state, persisted to
   `localStorage`, hydrated on load) exposing `login`/`register`/`logout`; on a `401` from the
   client, clear session and redirect to `/login`. Wire TanStack Query's `QueryClientProvider`.
   _Commit: session plumbing._
4. **Routing + layout.** React Router with public `/login`, `/register` and a
   `RequireAuth` wrapper guarding the app shell (nav + sign-out + the transactions route).
   Redirect authenticated users away from the auth pages. _Commit: navigable shell._
5. **Auth screens (FR-01, FR-02).** Register + login forms (React Hook Form + Zod mirroring
   the **12–128 char, upper/lower/digit, no-whitespace** password policy). Map `400.errors`
   to fields; show `409` (duplicate email) and `401` (bad credentials) as form-level errors.
   Sign-out clears the stored token. _Commit: working auth UI._
6. **Transactions list (FR-07).** Table backed by `GET /transactions` via a `useTransactions`
   query hook; filter (ticker, type, date range) + sort (column headers → `sortBy`/`descending`)
   + paging (`page`/`pageSize`, render `totalCount`). Loading / empty / error states.
   _Commit: read UI._
7. **Transactions create/edit/delete (FR-05, FR-06).** Add/edit form (Zod: quantity > 0,
   price > 0, ISO-4217 currency, ticker ≤ 20, no future date) → `POST`/`PUT` mutations;
   delete with a confirm dialog → `DELETE`. Invalidate the list query on success; surface
   `422` guard violations (over-sell / negative holding) as a form-level error.
   _Commit: full CRUD UI._
8. **Component/integration tests.** Vitest + RTL + MSW: auth form validation + error mapping,
   protected-route redirect, transactions list (filter/sort/paging), and the create→list and
   delete flows against mocked endpoints. _Commit: frontend tests green._
9. **CI (NFR-07).** Extend `.github/workflows/ci.yml` with a frontend job
   (`install → lint → build → test`), path-filtered to `frontend/**`. _Commit: CI covers FE._

**Requirements this slice moves:** FR-01/FR-02 → usable UI (incl. logout in the client);
FR-05/06/07 → usable UI; NFR-06 → frontend in the repo; NFR-07 → CI covers the frontend.

**Out of scope (deferred to later slices):** dashboard/portfolio-summary screens
(FR-13–FR-17), charts (FR-18–FR-21), demo-session "Try the demo" entry (FR-04), and any
base-currency conversion display — their backends don't exist yet.

### Deployment fit (Railway API + DB, Vercel frontend — full deploy is item 7)

The stack (axios + TanStack Query + RHF/Zod + Vite) compiles to a **static bundle with no
server runtime**, so Vercel just serves files and Railway hosts the API/DB. Build the slice
so this split is friction-free later — the deployment-critical pieces all live at the
frontend↔backend seam, not in the libraries:

- **API base URL is the only host reference.** Read it once from `import.meta.env.VITE_API_BASE_URL`
  in the axios instance — never hard-code a URL elsewhere. Vite **inlines `VITE_*` vars at
  build time**, so each environment needs its own value (local `.env` → `http://localhost:5029`;
  Vercel prod env → the Railway API URL) and changing it requires a rebuild. Commit
  `.env.example` documenting the var.
- **CORS is config-driven (the F1 prereq).** Frontend and API are different origins in prod,
  so the API must allow the Vercel origin(s) + the `Authorization` header via
  `Cors:AllowedOrigins` (a Railway env var) — no code change between environments. ⚠️ Vercel
  **preview deployments use dynamic `*.vercel.app` subdomains**; allow a wildcard/regex for
  those in non-prod config, or point previews at a staging API only.
- **Bearer-token-in-header auth (not cookies) is deliberately deploy-friendly.** Cross-site
  Vercel↔Railway calls avoid all `SameSite`/CSRF/cookie-domain issues; the token just rides
  in the header the axios interceptor attaches. (Accepts the usual `localStorage`/XSS
  trade-off.)
- **SPA fallback on Vercel.** Client-side routes need deep links / hard refreshes to serve
  `index.html`. Add `vercel.json` with a catch-all rewrite to `/index.html` so e.g.
  `/transactions` doesn't 404.
- **No SSR / serverless functions** — TanStack Query is a pure client cache, so Vercel serves
  static output only (no cold starts, no Node runtime to provision).

---

## Feature sequence after auth

1. ✅ **Transactions CRUD** (FR-05, FR-06, FR-07) — _done; see the slice section above._
   First real per-user feature; established the auth/ownership pattern.
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
- **Align EF Core package versions (cleanup)** — the unit-test project surfaces an
  `MSB3277` conflict between `Microsoft.EntityFrameworkCore.Relational` 10.0.4 and 10.0.9
  (pulled in transitively via the API/Infrastructure references). Harmless today; pin a
  single version across projects to clear the warning.

---

## Key rules to honor while building

- Vertical slices: Domain → Application (command/query + handler + validator + any new
  interface) → Infrastructure impl → API endpoint.
- CQRS via MediatR; thin controllers; no business logic in controllers or EF configs.
- P&L uses **weighted average cost** (not FIFO). Monetary values stored in original
  transaction currency; converted at display time using stored FX rates.
