# Portfolio App — Requirements

## Tech Stack

| Layer       | Technology                              |
|-------------|-----------------------------------------|
| Backend     | ASP.NET Core 10 Web API + MediatR + EF Core |
| Frontend    | React + TypeScript + Recharts           |
| Database    | PostgreSQL                              |
| Scheduler   | Hangfire                                |
| Market data | Alpha Vantage API                       |
| Hosting     | Railway (backend + DB) + Vercel (frontend) |

---

## Implementation Status

Legend: **✅ implemented** · **🟡 partially implemented** · _unmarked = not yet started_.
Status as of 2026-06-25 (after the authentication slice + CI pipeline). This reflects what
is actually built and tested, not what is merely scaffolded.

---

## Functional Requirements

### Authentication & Users

- **FR-01:** ✅ Users can register with email and password
- **FR-02:** 🟡 Users can log in and log out — _login implemented; logout not (stateless JWT, no revocation/endpoint yet)_
- **FR-03:** Each user sees only their own portfolio data — _not started: no portfolio data endpoints or per-user scoping exist yet (the JWT carries the user id as groundwork)_
- **FR-04:** Demo account
  - A built-in demo account is available without registration
  - The demo portfolio is pre-populated with a predefined set of transactions and assets
  - All application features are fully accessible in the demo session
  - Each demo login creates an isolated session with its own portfolio copy — multiple users can be logged in simultaneously under the demo account without interfering with each other
  - A demo session expires after 60 minutes, at which point the session portfolio is permanently discarded
  - The original demo portfolio template is never modified — it serves only as a read-only seed

### Portfolio & Transactions

- **FR-05:** User can add a transaction (buy or sell) for a stock or ETF, specifying ticker, quantity, price per share, currency, and date
- **FR-06:** User can edit or delete an existing transaction
- **FR-07:** User can view the full transaction history, sorted and filtered by asset or date

### Market Data

- **FR-08:** The system fetches current market prices for all held assets from an external API (e.g. Alpha Vantage)
- **FR-09:** Prices are refreshed automatically on a schedule (e.g. every 15 minutes during market hours)
- **FR-10:** The system supports assets quoted in different currencies (USD, EUR, GBP, etc.)

### Multi-currency

- **FR-11:** User sets a base display currency (e.g. PLN)
- **FR-12:** The system fetches current FX rates and converts all asset values to the base currency
- **FR-13:** Portfolio summary values (current value, cost basis, P&L) are always shown in the base currency

### Portfolio Summary

- **FR-14:** Dashboard shows total current portfolio value in the base currency
- **FR-15:** Dashboard shows total cost basis (amount invested)
- **FR-16:** Dashboard shows total profit or loss in absolute value and as a percentage
- **FR-17:** Each asset row shows: ticker, quantity, average buy price, current price, current value, P&L (absolute and %)

### Charts & Visualizations

- **FR-18:** Portfolio value over time chart (line chart, date range selectable)
- **FR-19:** Allocation pie chart showing each asset's share of the total portfolio by current value
- **FR-20:** Per-asset performance chart showing price history for a selected asset
- **FR-21:** Profit / loss history chart showing realized and unrealized P&L over time

---

## Non-Functional Requirements

- **NFR-01:** 🟡 REST API documented with Swagger / OpenAPI — _OpenAPI + Scalar wired; auth endpoints documented (XML + ProducesResponseType); coverage grows as endpoints land_
- **NFR-02:** Backend response time under 500ms for all non-external-API endpoints — _not measured/verified_
- **NFR-03:** ✅ Passwords stored as hashed values (e.g. bcrypt)
- **NFR-04:** 🟡 All API endpoints protected with JWT authentication — _JWT auth fully wired (token validation + `UseAuthentication`), but no endpoint carries `[Authorize]` yet; the only endpoints (register/login) are intentionally anonymous_
- **NFR-05:** 🟡 Application deployable via Docker Compose (backend + frontend + database) — _compose covers Postgres (+ pgAdmin) for local dev; backend and frontend services not yet included_
- **NFR-06:** 🟡 Backend and frontend code hosted in a public GitHub repository — _backend on GitHub (`Hansin86/Portfolio-.NET-React`); frontend not yet in the repo_
- **NFR-07:** ✅ CI pipeline (GitHub Actions) runs build and unit tests on every push — _workflow built and verified locally (build + unit + integration); on branch `ci/github-actions-pipeline`, active once merged to main_

---

## Demo Account — Technical Notes

- On every demo login, a temporary portfolio is created by copying the read-only `demo_portfolio_template`
- Each copy is identified by a unique `demo_session_id`
- JWT token for demo sessions includes claims: `"is_demo": true` and `"demo_session_id"`
- A Hangfire background job runs periodically to delete expired demo sessions (older than 60 minutes)
- The demo template is seeded once at application startup and never written to at runtime
