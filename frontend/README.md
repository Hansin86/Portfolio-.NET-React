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

## Project structure

```
src/
  main.tsx              # entry point — mounts <App>, wires QueryClientProvider + AuthProvider
  App.tsx               # BrowserRouter + route table (public vs. protected)
  index.css             # global styles + light/dark theme variables

  api/                  # everything that talks to the backend
    client.ts           # the single axios instance; bearer-token + global 401 interceptors
    types.ts            # response/domain types mirroring the API (AuthResponse, TransactionDto…)
    requests.ts         # request payload types (RegisterRequest, CreateTransactionRequest…)
    errors.ts           # ApiError + normalizeError (RFC 7807 problem-details → one error shape)
    auth.ts             # POST /auth/register | /auth/login
    transactions.ts     # transactions CRUD calls
    queryClient.ts      # TanStack Query client
    index.ts            # public surface of the api module

  auth/                 # signed-in session (React Context + localStorage)
    AuthProvider.tsx    # owns the session; syncs the axios token; registers the 401 handler
    AuthContext.ts      # context + useAuth() hook
    session.ts          # load/save/clear the token+user in localStorage

  routes/               # navigation
    paths.ts            # single source of truth for route paths
    guards.tsx          # RequireAuth / RedirectIfAuthenticated route gates
    AppLayout.tsx       # authenticated shell (nav + logout) wrapping protected pages

  pages/                # one component per route screen
    LoginPage.tsx       # login form (RHF + Zod)
    RegisterPage.tsx    # registration form (RHF + Zod)
    TransactionsPage.tsx
    auth/               # shared building blocks for the auth screens
      schemas.ts        # Zod schemas mirroring the backend password policy
      apiFormErrors.ts  # maps an ApiError → field errors (400) or a form-level banner (401/409)
      AuthForm.module.css
```

Conventions: server state goes through **TanStack Query**; forms use **React Hook Form +
Zod** (the schema mirrors the backend validators for instant feedback, but the server stays
the source of truth); the session lives in **React Context + localStorage**; styling is
**CSS Modules** (`*.module.css`) using the theme variables in `index.css`. Tests sit next to
the code they cover (`*.test.ts`).

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
