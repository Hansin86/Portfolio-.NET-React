// Single source of truth for route paths, so links and redirects don't drift.
export const paths = {
  login: '/login',
  register: '/register',
  transactions: '/transactions',
} as const

/** Where an authenticated user lands by default. */
export const HOME_PATH = paths.transactions
