// TypeScript mirror of the PortfolioApp API contract (see docs/implementation-plan.md
// "API contract the client codes against"). JSON is camelCase; enums are strings.

export type TransactionType = 'Buy' | 'Sell'

export type AssetType = 'Stock' | 'Etf'

export type SortField = 'TransactionDate' | 'Ticker' | 'Quantity' | 'PricePerUnit' | 'Type'

/** Returned by POST /auth/register and POST /auth/login. */
export interface AuthResponse {
  userId: string
  email: string
  token: string
}

export interface TransactionDto {
  id: string
  ticker: string
  assetName: string
  type: TransactionType
  quantity: number
  pricePerUnit: number
  /** ISO-4217 currency code, e.g. "USD". */
  currency: string
  /** ISO date, YYYY-MM-DD. */
  transactionDate: string
}

/** Envelope returned by GET /transactions. */
export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

// ---- RFC 7807 problem details ----------------------------------------------

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
}

export interface ValidationProblemDetails extends ProblemDetails {
  /** Field name -> validation messages. */
  errors: Record<string, string[]>
}
