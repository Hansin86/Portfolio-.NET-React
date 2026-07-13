import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import type { AuthResponse } from '../api'
import { setAuthToken, setUnauthorizedHandler } from '../api'
import { AuthContext } from './AuthContext'
import type { AuthContextValue } from './AuthContext'
import { clearSession, loadSession, saveSession } from './session'
import type { AuthSession } from './session'

/**
 * Owns the signed-in session. Hydrates from localStorage on first render so a
 * refresh keeps the user logged in, keeps the axios bearer token in sync, and
 * registers the global 401 handler (an expired/invalid token tears the session
 * down). Routing-based redirect on logout arrives with RequireAuth in the next
 * commit; for now a 401 simply clears the session and the tree re-renders logged
 * out.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  // Lazy initializer: read storage once and attach the token before first paint,
  // so any request kicked off on mount already carries the bearer header.
  const [session, setSession] = useState<AuthSession | null>(() => {
    const restored = loadSession()
    setAuthToken(restored?.token ?? null)
    return restored
  })

  const login = useCallback((auth: AuthResponse) => {
    const next = saveSession(auth)
    setAuthToken(next.token)
    setSession(next)
  }, [])

  const logout = useCallback(() => {
    clearSession()
    setAuthToken(null)
    setSession(null)
  }, [])

  // Register the global 401 handler through a ref so the client always calls the
  // latest logout without us re-subscribing on every render.
  const logoutRef = useRef(logout)
  logoutRef.current = logout
  useEffect(() => {
    setUnauthorizedHandler(() => logoutRef.current())
    return () => setUnauthorizedHandler(null)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user: session?.user ?? null,
      isAuthenticated: session !== null,
      login,
      logout,
    }),
    [session, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
