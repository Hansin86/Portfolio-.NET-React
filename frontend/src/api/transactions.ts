import { apiClient } from './client'
import type {
  CreateTransactionRequest,
  TransactionQuery,
  UpdateTransactionRequest,
} from './requests'
import type { PagedResult, TransactionDto } from './types'

/** GET /transactions — filtered/sorted/paged list scoped to the caller (FR-07). */
export async function getTransactions(
  query: TransactionQuery = {},
): Promise<PagedResult<TransactionDto>> {
  const { data } = await apiClient.get<PagedResult<TransactionDto>>('/transactions', {
    params: query,
  })
  return data
}

/** GET /transactions/{id}. */
export async function getTransaction(id: string): Promise<TransactionDto> {
  const { data } = await apiClient.get<TransactionDto>(`/transactions/${id}`)
  return data
}

/** POST /transactions — returns the created transaction (201). */
export async function createTransaction(body: CreateTransactionRequest): Promise<TransactionDto> {
  const { data } = await apiClient.post<TransactionDto>('/transactions', body)
  return data
}

/** PUT /transactions/{id} — returns the updated transaction (200). */
export async function updateTransaction(
  id: string,
  body: UpdateTransactionRequest,
): Promise<TransactionDto> {
  const { data } = await apiClient.put<TransactionDto>(`/transactions/${id}`, body)
  return data
}

/** DELETE /transactions/{id} (204). */
export async function deleteTransaction(id: string): Promise<void> {
  await apiClient.delete(`/transactions/${id}`)
}
