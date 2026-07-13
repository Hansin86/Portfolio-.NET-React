// Persistence for the signed-in session. The token is a bearer JWT; alongside it
// we keep the minimal user identity the UI needs so a page refresh can rehydrate
// without a round-trip. localStorage (not a cookie) matches the bearer-in-header
// strategy — see docs/implementation-plan.md, frontend stack decisions.

import type { AuthResponse } from '../api'

/** Minimal signed-in identity — AuthResponse without the token. */
export interface AuthUser {
  userId: string
  email: string
}

export interface AuthSession {
  token: string
  user: AuthUser
}

const STORAGE_KEY = 'portfolio.auth'

/** Read and validate the persisted session, or null if absent/corrupt. */
export function loadSession(): AuthSession | null {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) return null

  try {
    const parsed: unknown = JSON.parse(raw)
    if (!isAuthSession(parsed)) {
      // Shape drifted (older build, tampering) — drop it rather than crash on read.
      localStorage.removeItem(STORAGE_KEY)
      return null
    }
    return parsed
  } catch {
    localStorage.removeItem(STORAGE_KEY)
    return null
  }
}

/** Persist the session. Derives the stored user from an AuthResponse. */
export function saveSession(auth: AuthResponse): AuthSession {
  const session: AuthSession = {
    token: auth.token,
    user: { userId: auth.userId, email: auth.email },
  }
  localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
  return session
}

export function clearSession(): void {
  localStorage.removeItem(STORAGE_KEY)
}

function isAuthSession(value: unknown): value is AuthSession {
  if (typeof value !== 'object' || value === null) return false
  const candidate = value as Record<string, unknown>
  const user = candidate.user
  if (typeof user !== 'object' || user === null) return false
  const userFields = user as Record<string, unknown>
  return (
    typeof candidate.token === 'string' &&
    typeof userFields.userId === 'string' &&
    typeof userFields.email === 'string'
  )
}
