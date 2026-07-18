import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  transactionsApi,
  type CreateTransactionRequest,
  type TransactionDto,
  type UpdateTransactionRequest,
} from '../../api'
import { transactionKeys } from './useTransactions'

// Mutations for the transactions slice (FR-05, FR-06). Each invalidates every cached
// transactions list on success so the table reflects the change without a manual refetch.
// The axios interceptor already rejects with a normalized ApiError, so callers get the
// 400 field errors / 422 over-sell message straight off `mutation.error`.

export function useCreateTransaction() {
  const queryClient = useQueryClient()
  return useMutation<TransactionDto, unknown, CreateTransactionRequest>({
    mutationFn: (body) => transactionsApi.createTransaction(body),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: transactionKeys.all }),
  })
}

export function useUpdateTransaction() {
  const queryClient = useQueryClient()
  return useMutation<TransactionDto, unknown, { id: string; body: UpdateTransactionRequest }>({
    mutationFn: ({ id, body }) => transactionsApi.updateTransaction(id, body),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: transactionKeys.all }),
  })
}

export function useDeleteTransaction() {
  const queryClient = useQueryClient()
  return useMutation<void, unknown, string>({
    mutationFn: (id) => transactionsApi.deleteTransaction(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: transactionKeys.all }),
  })
}
