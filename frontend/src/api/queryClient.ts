import { QueryClient } from '@tanstack/react-query'
import { ApiError } from './errors'

/**
 * App-wide TanStack Query client. Server state (transactions, later the portfolio
 * summary) is cached here. Global 401 handling lives in the axios interceptor, so
 * queries don't retry an unauthorized request into a redirect loop.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: (failureCount, error) => {
        if (error instanceof ApiError && error.status === 401) return false
        return failureCount < 2
      },
      staleTime: 30_000,
      refetchOnWindowFocus: false,
    },
  },
})
