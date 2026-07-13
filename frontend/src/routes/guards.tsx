import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../auth'
import { HOME_PATH, paths } from './paths'

/** Location we bounce an unauthenticated user back from, so we can return them post-login. */
interface FromState {
  from?: string
}

/**
 * Gate for the protected area. Unauthenticated users are redirected to /login,
 * remembering where they were headed so login can send them back. This is what
 * makes a global 401 (which clears the session in AuthProvider) surface as a
 * redirect: the tree re-renders logged out and this guard takes over.
 */
export function RequireAuth() {
  const { isAuthenticated } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) {
    const from: FromState = { from: location.pathname + location.search }
    return <Navigate to={paths.login} state={from} replace />
  }
  return <Outlet />
}

/**
 * Inverse gate for /login and /register: an already-authenticated user has no
 * business on the auth screens, so send them to where they came from (or home).
 */
export function RedirectIfAuthenticated() {
  const { isAuthenticated } = useAuth()
  const location = useLocation()

  if (isAuthenticated) {
    const state = location.state as FromState | null
    return <Navigate to={state?.from ?? HOME_PATH} replace />
  }
  return <Outlet />
}
