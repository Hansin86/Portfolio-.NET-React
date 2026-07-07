// Request payloads sent to the PortfolioApp API (see docs/implementation-plan.md
// "API contract the client codes against"). Response/domain shapes live in ./types.

import type { AssetType, TransactionType, SortField } from './types'

export interface RegisterRequest {
  email: string
  password: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface CreateTransactionRequest {
  ticker: string
  type: TransactionType
  quantity: number
  pricePerUnit: number
  currency: string
  transactionDate: string
  assetType?: AssetType
}

/** PUT /transactions/{id} — ticker/asset are not editable. */
export interface UpdateTransactionRequest {
  type: TransactionType
  quantity: number
  pricePerUnit: number
  currency: string
  transactionDate: string
}

/** Query-string params for GET /transactions (FR-07 filter/sort/page). */
export interface TransactionQuery {
  assetTicker?: string
  type?: TransactionType
  fromDate?: string
  toDate?: string
  sortBy?: SortField
  descending?: boolean
  page?: number
  pageSize?: number
}
