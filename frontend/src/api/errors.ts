import { AxiosError } from 'axios'
import type { ProblemDetails, ValidationProblemDetails } from './types'

/**
 * Normalized API error. The response interceptor converts every failed request
 * (RFC 7807 problem+json, a network failure, etc.) into one of these so the UI
 * has a single error shape to reason about.
 */
export class ApiError extends Error {
  /** HTTP status, or null for network/transport failures. */
  readonly status: number | null
  /** Parsed problem-details body when present. */
  readonly problem: ProblemDetails | null
  /** Per-field validation messages from a 400 ValidationProblemDetails. */
  readonly validationErrors: Record<string, string[]> | null

  constructor(
    message: string,
    status: number | null,
    problem: ProblemDetails | null,
    validationErrors: Record<string, string[]> | null,
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
    this.validationErrors = validationErrors
  }

  /** True for a 400 carrying an `errors` map to bind to form fields. */
  get isValidation(): boolean {
    return this.validationErrors !== null
  }

  get isUnauthorized(): boolean {
    return this.status === 401
  }
}

function isProblemDetails(data: unknown): data is ProblemDetails {
  return typeof data === 'object' && data !== null
}

function hasErrorsMap(data: ProblemDetails): data is ValidationProblemDetails {
  const errors = (data as ValidationProblemDetails).errors
  return typeof errors === 'object' && errors !== null
}

/** Convert any thrown value from axios into an {@link ApiError}. */
export function normalizeError(error: unknown): ApiError {
  if (error instanceof ApiError) return error

  if (error instanceof AxiosError) {
    const response = error.response
    if (response) {
      const data: unknown = response.data
      const problem = isProblemDetails(data) ? data : null
      const validationErrors = problem && hasErrorsMap(problem) ? problem.errors : null
      const message =
        problem?.detail ?? problem?.title ?? error.message ?? `Request failed (${response.status})`
      return new ApiError(message, response.status, problem, validationErrors)
    }
    // No response: timeout, DNS, CORS, offline, etc.
    return new ApiError(
      error.message || 'Network error — could not reach the server.',
      null,
      null,
      null,
    )
  }

  const message = error instanceof Error ? error.message : 'Unexpected error'
  return new ApiError(message, null, null, null)
}
