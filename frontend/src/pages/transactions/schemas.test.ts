import { describe, expect, it } from 'vitest'
import { transactionFormSchema } from './schemas'

// Mirrors the backend AddTransactionCommandValidator; keep messages in sync with schemas.ts.
const valid = {
  ticker: 'AAPL',
  type: 'Buy',
  quantity: '10',
  pricePerUnit: '150.5',
  currency: 'USD',
  transactionDate: '2020-01-02',
  assetType: 'Stock',
}

describe('transactionFormSchema', () => {
  it('accepts a well-formed transaction', () => {
    expect(transactionFormSchema.safeParse(valid).success).toBe(true)
  })

  it('accepts a lowercase currency code (upper-cased at submit)', () => {
    expect(transactionFormSchema.safeParse({ ...valid, currency: 'usd' }).success).toBe(true)
  })

  it.each([
    ['empty ticker', { ticker: '' }, 'Ticker is required'],
    ['over-long ticker', { ticker: 'A'.repeat(21) }, 'must not exceed 20'],
    ['zero quantity', { quantity: '0' }, 'Quantity must be greater than 0'],
    ['negative price', { pricePerUnit: '-5' }, 'Price per unit must be greater than 0'],
    ['two-letter currency', { currency: 'US' }, '3-letter ISO'],
  ])('rejects %s', (_label, patch, fragment) => {
    const result = transactionFormSchema.safeParse({ ...valid, ...patch })
    expect(result.success).toBe(false)
    if (!result.success) {
      const messages = result.error.issues.map((issue) => issue.message).join(' ')
      expect(messages).toContain(fragment)
    }
  })

  it('rejects a future transaction date', () => {
    const tomorrow = new Date(Date.now() + 86_400_000).toISOString().slice(0, 10)
    const result = transactionFormSchema.safeParse({ ...valid, transactionDate: tomorrow })
    expect(result.success).toBe(false)
  })
})
