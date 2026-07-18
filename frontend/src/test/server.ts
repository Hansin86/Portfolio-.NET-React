import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import type { TransactionDto } from '../api'

// A small MSW mock of the transactions API backed by an in-memory store, so integration
// tests exercise the real request path (axios → interceptors → TanStack Query) and see the
// list reflect writes after invalidation. Tests seed the store via `seedTransactions` and
// can override a single endpoint with `server.use(...)` (e.g. to force a 422).

const BASE = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:5029'

let db: TransactionDto[] = []

/** Replace the store — call at the start of a test to define its starting data. */
export function seedTransactions(items: TransactionDto[]): void {
  db = [...items]
}

/** Empty the store — called between tests by the global setup. */
export function resetDb(): void {
  db = []
}

export const handlers = [
  http.get(`${BASE}/transactions`, ({ request }) => {
    const url = new URL(request.url)
    const type = url.searchParams.get('type')
    const ticker = url.searchParams.get('assetTicker')
    const page = Number(url.searchParams.get('page') ?? '1')
    const pageSize = Number(url.searchParams.get('pageSize') ?? '20')

    let items = [...db]
    if (type) items = items.filter((tx) => tx.type === type)
    if (ticker) items = items.filter((tx) => tx.ticker.toLowerCase().includes(ticker.toLowerCase()))

    const totalCount = items.length
    const start = (page - 1) * pageSize
    return HttpResponse.json({
      items: items.slice(start, start + pageSize),
      totalCount,
      page,
      pageSize,
    })
  }),

  http.post(`${BASE}/transactions`, async ({ request }) => {
    const body = (await request.json()) as Record<string, unknown>
    const created: TransactionDto = {
      id: crypto.randomUUID(),
      ticker: String(body.ticker),
      assetName: `${String(body.ticker)} Inc.`,
      type: body.type as TransactionDto['type'],
      quantity: Number(body.quantity),
      pricePerUnit: Number(body.pricePerUnit),
      currency: String(body.currency),
      transactionDate: String(body.transactionDate),
    }
    db = [created, ...db]
    return HttpResponse.json(created, { status: 201 })
  }),

  http.put(`${BASE}/transactions/:id`, async ({ params, request }) => {
    const body = (await request.json()) as Record<string, unknown>
    db = db.map((tx) =>
      tx.id === params.id ? ({ ...tx, ...body, id: tx.id } as TransactionDto) : tx,
    )
    return HttpResponse.json(db.find((tx) => tx.id === params.id))
  }),

  http.delete(`${BASE}/transactions/:id`, ({ params }) => {
    db = db.filter((tx) => tx.id !== params.id)
    return new HttpResponse(null, { status: 204 })
  }),
]

export const server = setupServer(...handlers)
