import { useEffect, useMemo, useState } from 'react'
import { normalizeError, type SortField, type TransactionQuery, type TransactionType } from '../api'
import styles from './transactions/TransactionsList.module.css'
import { useTransactions } from './transactions/useTransactions'

const PAGE_SIZE = 20

const numberFmt = new Intl.NumberFormat(undefined, { maximumFractionDigits: 8 })

/** Currency-aware money formatting, falling back to "1,234.5 XYZ" for unknown codes. */
function formatMoney(value: number, currency: string): string {
  try {
    return new Intl.NumberFormat(undefined, { style: 'currency', currency }).format(value)
  } catch {
    return `${numberFmt.format(value)} ${currency}`
  }
}

/** Debounce a rapidly-changing value (the ticker text box) so we don't refetch per keystroke. */
function useDebouncedValue<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = useState(value)
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs)
    return () => clearTimeout(timer)
  }, [value, delayMs])
  return debounced
}

/**
 * Transactions list (FR-07): filter by ticker/type/date range, sort by clickable column,
 * and page through results. Server state comes from `useTransactions`; loading, empty, and
 * error states are handled explicitly. Create/edit/delete arrive in the next commit.
 */
export function TransactionsPage() {
  const [tickerInput, setTickerInput] = useState('')
  const ticker = useDebouncedValue(tickerInput, 300)
  const [type, setType] = useState<TransactionType | ''>('')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [sortBy, setSortBy] = useState<SortField>('TransactionDate')
  const [descending, setDescending] = useState(true)
  const [page, setPage] = useState(1)

  // Any change to a filter or sort should return to the first page.
  useEffect(() => {
    setPage(1)
  }, [ticker, type, fromDate, toDate, sortBy, descending])

  const query: TransactionQuery = useMemo(
    () => ({
      assetTicker: ticker.trim() || undefined,
      type: type || undefined,
      fromDate: fromDate || undefined,
      toDate: toDate || undefined,
      sortBy,
      descending,
      page,
      pageSize: PAGE_SIZE,
    }),
    [ticker, type, fromDate, toDate, sortBy, descending, page],
  )

  const { data, isPending, isError, error, isFetching, refetch } = useTransactions(query)

  function toggleSort(field: SortField) {
    if (sortBy === field) {
      setDescending((prev) => !prev)
    } else {
      setSortBy(field)
      setDescending(false)
    }
  }

  function resetFilters() {
    setTickerInput('')
    setType('')
    setFromDate('')
    setToDate('')
  }

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const hasActiveFilters = ticker.trim() !== '' || type !== '' || fromDate !== '' || toDate !== ''

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <h1 className={styles.title}>Transactions</h1>
        {data && (
          <span className={styles.count}>
            {data.totalCount} {data.totalCount === 1 ? 'transaction' : 'transactions'}
          </span>
        )}
      </div>

      <div className={styles.filters}>
        <div className={styles.filter}>
          <label className={styles.filterLabel} htmlFor="filter-ticker">
            Ticker
          </label>
          <input
            id="filter-ticker"
            className={styles.control}
            placeholder="e.g. AAPL"
            value={tickerInput}
            onChange={(e) => setTickerInput(e.target.value)}
          />
        </div>

        <div className={styles.filter}>
          <label className={styles.filterLabel} htmlFor="filter-type">
            Type
          </label>
          <select
            id="filter-type"
            className={styles.control}
            value={type}
            onChange={(e) => setType(e.target.value as TransactionType | '')}
          >
            <option value="">All</option>
            <option value="Buy">Buy</option>
            <option value="Sell">Sell</option>
          </select>
        </div>

        <div className={styles.filter}>
          <label className={styles.filterLabel} htmlFor="filter-from">
            From
          </label>
          <input
            id="filter-from"
            type="date"
            className={styles.control}
            value={fromDate}
            max={toDate || undefined}
            onChange={(e) => setFromDate(e.target.value)}
          />
        </div>

        <div className={styles.filter}>
          <label className={styles.filterLabel} htmlFor="filter-to">
            To
          </label>
          <input
            id="filter-to"
            type="date"
            className={styles.control}
            value={toDate}
            min={fromDate || undefined}
            onChange={(e) => setToDate(e.target.value)}
          />
        </div>

        {hasActiveFilters && (
          <button type="button" className={styles.reset} onClick={resetFilters}>
            Clear filters
          </button>
        )}
      </div>

      {isPending ? (
        <div className={styles.state}>Loading transactions…</div>
      ) : isError ? (
        <div className={`${styles.state} ${styles.error}`} role="alert">
          <p>{normalizeError(error).message}</p>
          <button type="button" className={styles.retry} onClick={() => refetch()}>
            Retry
          </button>
        </div>
      ) : data.items.length === 0 ? (
        <div className={styles.state}>
          {hasActiveFilters
            ? 'No transactions match these filters.'
            : 'No transactions yet. Add one to get started.'}
        </div>
      ) : (
        <div className={styles.tableWrap}>
          <table className={`${styles.table} ${isFetching ? styles.stale : ''}`}>
            <thead>
              <tr>
                <SortHeader
                  field="TransactionDate"
                  label="Date"
                  {...{ sortBy, descending, toggleSort }}
                />
                <SortHeader field="Ticker" label="Ticker" {...{ sortBy, descending, toggleSort }} />
                <th>Name</th>
                <SortHeader field="Type" label="Type" {...{ sortBy, descending, toggleSort }} />
                <SortHeader
                  field="Quantity"
                  label="Quantity"
                  numeric
                  {...{ sortBy, descending, toggleSort }}
                />
                <SortHeader
                  field="PricePerUnit"
                  label="Price / unit"
                  numeric
                  {...{ sortBy, descending, toggleSort }}
                />
              </tr>
            </thead>
            <tbody>
              {data.items.map((tx) => (
                <tr key={tx.id}>
                  <td>{tx.transactionDate}</td>
                  <td>{tx.ticker}</td>
                  <td>{tx.assetName}</td>
                  <td>
                    <span
                      className={`${styles.type} ${tx.type === 'Buy' ? styles.buy : styles.sell}`}
                    >
                      {tx.type}
                    </span>
                  </td>
                  <td className={styles.numeric}>{numberFmt.format(tx.quantity)}</td>
                  <td className={styles.numeric}>{formatMoney(tx.pricePerUnit, tx.currency)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {data && data.totalCount > 0 && (
        <div className={styles.pager}>
          <span className={styles.pageInfo}>
            Page {data.page} of {totalPages}
          </span>
          <button
            type="button"
            className={styles.pageButton}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={data.page <= 1}
          >
            Previous
          </button>
          <button
            type="button"
            className={styles.pageButton}
            onClick={() => setPage((p) => p + 1)}
            disabled={data.page >= totalPages}
          >
            Next
          </button>
        </div>
      )}
    </div>
  )
}

interface SortHeaderProps {
  field: SortField
  label: string
  numeric?: boolean
  sortBy: SortField
  descending: boolean
  toggleSort: (field: SortField) => void
}

/** A clickable column header that shows and toggles the active sort direction. */
function SortHeader({ field, label, numeric, sortBy, descending, toggleSort }: SortHeaderProps) {
  const active = sortBy === field
  return (
    <th
      className={`${styles.sortable} ${numeric ? styles.numeric : ''}`}
      onClick={() => toggleSort(field)}
      aria-sort={active ? (descending ? 'descending' : 'ascending') : 'none'}
    >
      {label}
      {active && <span className={styles.sortArrow}> {descending ? '▼' : '▲'}</span>}
    </th>
  )
}
