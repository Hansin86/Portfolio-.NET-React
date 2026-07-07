# Portfolio Tracker — Frontend

React + TypeScript single-page app (Vite) for the portfolio tracker. Talks to the
ASP.NET Core API in `../src/PortfolioApp.API`.

## Prerequisites

- Node.js 20+ and npm
- The API running locally (see the repo root `README.md` / `CLAUDE.md`)

## Setup

```bash
cd frontend
npm install
cp .env.example .env   # then adjust VITE_API_BASE_URL if needed
```

`VITE_API_BASE_URL` points the app at the API. The default is `http://localhost:5029`
(the API's HTTP launch URL). Vite inlines `VITE_*` variables at **build time**, so
changing this value requires a restart of the dev server / a rebuild.

## Running alongside the API

Two terminals from the repo root:

```bash
# Terminal 1 — API (http://localhost:5029)
dotnet run --project src/PortfolioApp.API

# Terminal 2 — frontend dev server (http://localhost:5173)
cd frontend && npm run dev
```

The dev server runs on **http://localhost:5173**, which is the origin allowed by the
API's `SpaCors` policy (`Cors:AllowedOrigins`). If you change the frontend port, update
that config on the API too.

## Scripts

| Script                 | Purpose                                  |
| ---------------------- | ---------------------------------------- |
| `npm run dev`          | Start the Vite dev server with HMR       |
| `npm run build`        | Type-check (`tsc -b`) and build for prod |
| `npm run preview`      | Serve the production build locally       |
| `npm run lint`         | Lint with oxlint                         |
| `npm run format`       | Format the codebase with Prettier        |
| `npm run format:check` | Check formatting without writing         |
| `npm test`             | Run the test suite once (Vitest)         |
| `npm run test:watch`   | Run Vitest in watch mode                 |

## Tooling

- **Vite** + **@vitejs/plugin-react** — dev server, build
- **oxlint** — fast linting (`.oxlintrc.json`)
- **Prettier** — formatting (`.prettierrc.json`)
- **Vitest** — test runner (React Testing Library + MSW added in a later slice)
