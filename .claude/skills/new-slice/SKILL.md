---
name: new-slice
description: Scaffold a new vertical slice (feature) following this repo's Clean Architecture + CQRS conventions — Domain → Application (command/query + handler + validator + port) → Infrastructure repo → thin API controller → unit + integration tests. Use when adding a backend feature or a single operation (e.g. "add a GetPortfolioSummary query", "new endpoint to import transactions", "build the market-data slice"). Mirrors the Transactions CRUD slice, the cleanest reference implementation.
---

# Scaffold a vertical slice

Build a feature the way this repo already builds them. The **Transactions CRUD** slice is
the reference — when in doubt, open the matching file under
`src/PortfolioApp.Application/Features/Transactions/` and copy its shape.

Dependency arrow points inward: **API → Application → Domain**, **Infrastructure →
Application/Domain**. Domain depends on nothing. Never invert this.

## 0. Scope it first (decide before writing code)

- **One operation or several?** One folder per operation (`AddX`, `EditX`, `GetX`). A
  "slice" is usually several operations sharing a `Common/` folder.
- **Does Domain change?** New entity/field/rule ⇒ Domain edit **and an EF migration**
  (step 5). Pure read/query over existing tables ⇒ no migration.
- **New external dependency or persistence need?** ⇒ a new **port** (interface in
  `Application/Interfaces/`) + Infrastructure implementation. Reuse existing repos/ports
  when you can.
- **Per-user data?** Almost always yes ⇒ resolve the caller's portfolio and scope to it
  (see the ownership pattern below). Return **404** (not 403) for another user's resource
  so existence isn't leaked.

Confirm the scope and the FR/NFR ids with the user if unclear, then work the layers in
order below. Prefer several small commits (one per layer or per operation) — the plan file
tracks them; run `/update-plan` as they land.

## 1. Domain — `src/PortfolioApp.Domain`

Only if the feature needs new entities, value objects, enums, or invariants. Keep it pure
(no EF, no framework refs). New failure modes get an exception in `Domain/Exceptions/`
(`DomainException` for rule violations → 422; `NotFoundException` → 404 already exist —
reuse them). If you touched entities/config, a migration is due in step 5.

## 2. Application — `src/PortfolioApp.Application/Features/<Feature>/`

One folder per operation. Naming is strict: commands end `Command`, queries `Query`,
handlers `Handler`, DTOs `Dto`, validators `Validator`.

**Command / query** — a `record` implementing `IRequest<TResult>`, with an XML summary
citing the FR id. Example: `Features/Transactions/AddTransaction/AddTransactionCommand.cs`.

```csharp
public record AddTransactionCommand(string Ticker, /* … */) : IRequest<TransactionDto>;
```

**Handler** — `public class <Op>Handler : IRequestHandler<TReq, TRes>`, constructor
injection of ports + `IMapper`, XML summary. The per-user ownership pattern (copy verbatim
from `AddTransactionCommandHandler` / `EditTransactionCommandHandler`):

```csharp
Portfolio portfolio = await _portfolios.GetByUserIdAsync(_currentUser.UserId, ct)
    ?? throw new NotFoundException("No portfolio was found for the current user.");
// load entity, then: if (entity is null || entity.PortfolioId != portfolio.Id)
//     throw new NotFoundException(nameof(Transaction), request.Id);   // 404, not 403
```

- Business-rule violations ⇒ `throw new DomainException("…")` (→ 422). Don't return error
  objects; throw — the global middleware maps them.
- Currency is a value object: `Currency.From(request.Currency)` (throws `DomainException`
  on a bad code). Never store raw currency strings.
- Return a **DTO**, mapped via `IMapper` — handlers don't leak entities.

**Validator** — `AbstractValidator<TCommand>` next to the command; runs automatically via
the `ValidationBehaviour` pipeline (→ 400 on failure). Don't call it manually. Currency
rule: `Iso4217.Codes.Contains(code.Trim().ToUpperInvariant())` — one source of truth, no
regex. See `AddTransactionCommandValidator`.

**Shared per-feature types** go in `Features/<Feature>/Common/` — the `Dto`, any query
parameters, sort-field enums. Paged reads return `PagedResult<T>`
(`Application/Common/Models/`).

**DI is automatic:** MediatR handlers, FluentValidation validators, and AutoMapper
profiles are all assembly-scanned in `AddApplication()`. **Do not** register them by hand.

**AutoMapper profile** — in `Common/Mappings/<Entity>Profile.cs`. For record DTOs use
`ForCtorParam`; flatten related entities; expose currency as `.Code`. See
`TransactionProfile`.

## 3. Ports — `src/PortfolioApp.Application/Interfaces/`

Only for new persistence/external needs. Interface named `I…Repository` / `I…Client`, with
XML docs, `CancellationToken cancellationToken = default` on every method. This is where
the abstraction lives; the arrow stays inward. See `ITransactionRepository`.

## 4. Infrastructure — `src/PortfolioApp.Infrastructure/Repositories/`

Implement the port over `PortfolioDbContext`. Repos own their `SaveChangesAsync` calls
(see `TransactionRepository`). Lean on existing indexes for filtered/sorted queries; add a
`.ThenBy(x => x.Id)` tie-breaker to any sort backing pagination. **Register the impl
`scoped` in `AddInfrastructure()`** (`DependencyInjection.cs`) — this one *is* manual.
No business logic here.

## 5. Migration — only if Domain/EF config changed

```bash
dotnet ef migrations add <Name> --project src/PortfolioApp.Infrastructure --startup-project src/PortfolioApp.API
dotnet ef database update --project src/PortfolioApp.Infrastructure --startup-project src/PortfolioApp.API
```

Then confirm nothing else drifted: `dotnet ef migrations has-pending-model-changes …`
should report none. (Value-object converters like `CurrencyConverter` keep the same store
type, so they need no migration.)

## 6. API — `src/PortfolioApp.API/Controllers/`

Thin controller: `[ApiController]`, `[Authorize]`, `[Route("…")]`, inject `ISender _sender`,
each action just builds the request and `await _sender.Send(...)`. Model on
`TransactionsController`.

- **XML `<summary>` + `<response>` tags + `ProducesResponseType` on every action** — this
  is enforced by the "Do Not skip XML summary comments" rule and feeds OpenAPI/Scalar.
- Verbs: `POST` → `CreatedAtAction(...)` **201**; `GET` → `Ok(...)` **200** (`[FromQuery]`
  for query objects); `PUT` → `Ok(...)` **200**; `DELETE` → `NoContent()` **204**.
- Route id ≠ body: for `PUT`, bind a separate request contract (`API/Contracts/…Request`)
  and take the id from the route — don't trust an id in the body.
- Always accept and forward `CancellationToken`. No business logic, no external calls.

## 7. Tests

**Unit** — `tests/PortfolioApp.UnitTests/Features/<Feature>/<Operation>/`. xUnit +
FluentAssertions + NSubstitute (mock ports) + AutoFixture. Use a **real** mapper
(`new MapperConfiguration(cfg => cfg.AddProfile<XProfile>(), NullLoggerFactory.Instance)`)
rather than mocking `IMapper`. Cover: happy path, the ownership check (another user's id ⇒
`NotFoundException`), any guard (⇒ `DomainException`), and every validator rule. See
`AddTransactionCommandHandlerTests`.

**Integration** — `tests/PortfolioApp.IntegrationTests/<Feature>/`. `WebApplicationFactory`
(`PortfolioApiFactory`) + Testcontainers Postgres (**Docker must be running**). Get an
authenticated client via `factory.CreateAuthenticatedClientAsync()`
(`Infrastructure/ApiClientExtensions.cs`). Assert the full lifecycle, plus the two
cross-cutting guarantees every protected slice must prove:
- **FR-03 isolation** — user A gets **404** for user B's resource.
- **NFR-04** — no bearer token ⇒ **401**.

## 8. Finish

```bash
dotnet build && dotnet test        # full suite must be green
```

Then run `/update-plan` to tick the slice's commits and refresh Current State. Don't commit
unless asked.

## Conventions cheat-sheet

- `var` is banned for non-obvious types — write the type.
- Throw domain exceptions; don't return error codes. Middleware maps
  `NotFoundException`→404, `DomainException`→422, `ValidationException`→400.
- No business logic in controllers or EF configs.
- Enums serialize as **names** globally (already configured) — no per-endpoint work.
- Money is stored in original transaction currency; convert only at display time. P&L uses
  **weighted average cost**, never FIFO.
