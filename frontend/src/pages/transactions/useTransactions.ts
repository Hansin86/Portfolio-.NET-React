import { keepPreviousData, useQuery } from '@tanstack/react-query'
import {
  transactionsApi,
  type PagedResult,
  type TransactionDto,
  type TransactionQuery,
} from '../../api'

/**
 * Query-key factory for the transactions cache. `list(query)` keys each filter/sort/page
 * combination separately so switching pages or filters caches independently and the
 * create/edit/delete slice can invalidate `all` to refresh every list at once.
 */
export const transactionKeys = {
  all: ['transactions'] as const,
  list: (query: TransactionQuery) => [...transactionKeys.all, 'list', query] as const,
}

/**
 * Fetches the caller's filtered/sorted/paged transactions (GET /transactions, FR-07).
 * `placeholderData: keepPreviousData` holds the previous page on screen while the next
 * one loads, so paging and filtering don't flash an empty table.
 */
export function useTransactions(query: TransactionQuery) {
  return useQuery<PagedResult<TransactionDto>>({
    queryKey: transactionKeys.list(query),
    queryFn: () => transactionsApi.getTransactions(query),
    placeholderData: keepPreviousData,
  })
}
