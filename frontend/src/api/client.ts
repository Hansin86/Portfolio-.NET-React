import axios from 'axios'
import { normalizeError } from './errors'

const baseURL = import.meta.env.VITE_API_BASE_URL
if (!baseURL) {
  // Fail loud in dev: a missing base URL means every request 404s against the SPA host.
  console.error('VITE_API_BASE_URL is not set — API requests will fail. See frontend/.env.example.')
}

/**
 * The single axios instance the whole app uses. Base URL comes from the build-time
 * VITE_API_BASE_URL env var; auth token and global 401 handling are attached via the
 * setters below so this module stays decoupled from the auth/session layer (step 3).
 */
export const apiClient = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
})

// --- Bearer token -----------------------------------------------------------
// Held in memory; the auth layer calls setAuthToken() on login and on hydration
// from storage, and setAuthToken(null) on logout.
let authToken: string | null = null

export function setAuthToken(token: string | null): void {
  authToken = token
}

apiClient.interceptors.request.use((config) => {
  if (authToken) {
    config.headers.Authorization = `Bearer ${authToken}`
  }
  return config
})

// --- Global 401 handling ----------------------------------------------------
// The auth layer registers a handler (clear session + redirect to /login). We
// still reject with a normalized ApiError so the caller can react too.
let unauthorizedHandler: (() => void) | null = null

export function setUnauthorizedHandler(handler: (() => void) | null): void {
  unauthorizedHandler = handler
}

apiClient.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    const apiError = normalizeError(error)
    if (apiError.status === 401 && unauthorizedHandler) {
      unauthorizedHandler()
    }
    return Promise.reject(apiError)
  },
)
