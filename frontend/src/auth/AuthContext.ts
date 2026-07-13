import { createContext, useContext } from 'react'
import type { AuthResponse } from '../api'
import type { AuthUser } from './session'

export interface AuthContextValue {
  /** The signed-in user, or null when logged out. */
  user: AuthUser | null
  /** Convenience flag; true when a session is active. */
  isAuthenticated: boolean
  /** Persist a session from an auth response and attach the bearer token. */
  login: (auth: AuthResponse) => void
  /** Clear the session and detach the bearer token. */
  logout: () => void
}

// Kept in its own module (separate from AuthProvider) so the provider file only
// exports a component — satisfies react-refresh / only-export-components.
export const AuthContext = createContext<AuthContextValue | null>(null)

/** Access the auth session. Throws if used outside <AuthProvider>. */
export function useAuth(): AuthContextValue {
  const value = useContext(AuthContext)
  if (value === null) {
    throw new Error('useAuth must be used within an <AuthProvider>')
  }
  return value
}
