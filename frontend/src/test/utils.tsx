import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, type RenderResult } from '@testing-library/react'
import type { ReactElement } from 'react'
import type { TransactionDto } from '../api'

/**
 * Render a component inside a fresh TanStack Query client with retries off (so a mocked
 * error surfaces immediately instead of being retried).
 */
export function renderWithClient(ui: ReactElement): RenderResult {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>)
}

let seq = 0

/** Build a TransactionDto with sensible defaults; override any field per test. */
export function makeTransaction(overrides: Partial<TransactionDto> = {}): TransactionDto {
  seq += 1
  return {
    id: `tx-${seq}`,
    ticker: 'AAPL',
    assetName: 'Apple Inc.',
    type: 'Buy',
    quantity: 10,
    pricePerUnit: 150,
    currency: 'USD',
    transactionDate: '2024-01-02',
    ...overrides,
  }
}
