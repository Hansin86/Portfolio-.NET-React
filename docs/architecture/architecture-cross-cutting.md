# Architecture — Cross-Cutting Concerns

Part of the architecture reference (see `architecture-layers.md` for the layering and
`architecture-request-lifecycle.md` for how a request flows). This document covers the
concerns that apply across *every* request rather than to a single feature:
authentication, validation, error handling, logging, and API documentation.

```mermaid
flowchart TD
    REQ["HTTP request"] --> LOG["Serilog request logging (planned)"]
    LOG --> EXC["Exception middleware (planned)"]
    EXC --> AUTH["JWT auth + authorization (planned)"]
    AUTH --> PIPE["MediatR pipeline"]
    PIPE --> VAL["Validation behavior (planned)"]
    VAL --> HANDLER["Handler"]
    DOCS["OpenAPI + Scalar (wired)"] -. "describes endpoints" .-> AUTH
```

---

## Authentication & authorization — JWT *(planned)*

- **Scheme:** JWT bearer. NFR-04 requires **all** endpoints protected, except the public
  entry points (`register`, `login`, `demo`).
- **Issuing:** an `IJwtTokenGenerator` port in Application, implemented in Infrastructure,
  produces signed tokens on login. Passwords are verified against bcrypt hashes via
  `IPasswordHasher` (NFR-03).
- **Validating:** `AddAuthentication().AddJwtBearer(...)` validates the
  `Authorization: Bearer <token>` header and populates `HttpContext.User` claims; handlers
  read identity from those claims to scope data per user (FR-03).
- **Demo claims:** demo tokens carry `is_demo = true` and `demo_session_id`, used to route
  requests to the isolated demo portfolio.
- **Current state:** `Program.cs` calls `UseAuthorization()` but **no authentication
  scheme is registered yet**, and `UseAuthentication()` is not yet in the pipeline. The
  `Microsoft.AspNetCore.Authentication.JwtBearer` package is referenced.

## Input validation — FluentValidation *(planned)*

- A MediatR **pipeline behavior** (in `Application/Common/Behaviours/`) runs all
  registered validators before the handler executes; failures throw `ValidationException`,
  surfaced as `400` by the exception middleware.
- Validators live next to their command/query. Handlers can then assume valid input.
- **Current state:** `Common/Behaviours/` exists but is empty; the
  `FluentValidation.DependencyInjectionExtensions` package is referenced in Application.

## Error handling — exception middleware *(planned)*

- A single middleware wraps the pipeline and converts exceptions into consistent
  **problem-details JSON** instead of leaking stack traces.
- Custom exceptions in `Domain/Exceptions/` map to status codes (e.g. not-found → 404,
  validation → 400, unauthorized → 401). This keeps `try/catch` out of controllers and
  handlers.
- **Current state:** the `API/Middleware/` and `Domain/Exceptions/` folders exist but are
  empty.

## Object mapping — AutoMapper *(planned)*

- Entity ↔ DTO mapping is centralized in AutoMapper profiles under
  `Application/Common/Mappings/`, so handlers return DTOs and entities never cross the API
  boundary.
- **Current state:** `Common/Mappings/` exists but is empty; AutoMapper is referenced in
  Application, Infrastructure, and API.

## Logging — Serilog *(planned)*

- **Serilog** for structured logging and request logging (`UseSerilogRequestLogging`),
  replacing the default provider, so logs carry structured properties (request path,
  status, timing, user) usable in any sink.
- **Current state:** `Serilog.AspNetCore` is referenced in the API, but `Program.cs` still
  uses the default logging config in `appsettings.json` — Serilog is **not yet wired**.

## API documentation — OpenAPI + Scalar *(wired)*

- The built-in **OpenAPI** document (`AddOpenApi()` / `MapOpenApi()`) is served at
  `/openapi/v1.json`, and **Scalar** renders interactive docs at `/scalar/v1`
  (Development only) — NFR-01.
- XML summary comments on public controller actions feed the OpenAPI document, so keeping
  them current keeps the docs current.
- **Current state:** **active** — both are mapped in `Program.cs` under
  `IsDevelopment()`. This is the one cross-cutting concern fully in place today.

---

## Where each concern is wired

All cross-cutting wiring lives in the **API composition root** (`Program.cs` plus
`API/Extensions/` DI helpers), pulling in `AddApplication()` *(planned)* and
`AddInfrastructure()` *(wired)*. That keeps registration in one place and the dependency
flow one-directional (see `architecture-layers.md`).

| Concern | Layer it's defined in | Layer it's implemented/wired in | Status |
|---------|----------------------|---------------------------------|--------|
| JWT auth | Application (port) | Infrastructure + API | planned |
| Validation behavior | Application | Application + API (DI) | planned |
| Exception middleware | API | API | planned |
| AutoMapper profiles | Application | Application + API (DI) | planned |
| Serilog logging | — | API | planned |
| OpenAPI + Scalar | — | API | wired |
