import { z } from 'zod'

// Client-side mirror of the backend transaction validators (AddTransactionCommandValidator /
// EditTransactionCommandValidator). Messages match the server's so a value that slips past
// the client and fails server-side reads the same. The server stays the source of truth
// (full ISO 4217 check, over-sell guard) — this is just fast, friendly feedback.

/** Today in UTC as YYYY-MM-DD; ISO date strings compare correctly with `<=`. */
export function todayIso(): string {
  return new Date().toISOString().slice(0, 10)
}

/** A positive-decimal field kept as a string (what the input holds); converted at submit. */
const positiveAmount = (label: string) =>
  z
    .string()
    .min(1, `${label} is required.`)
    .refine((value) => Number(value) > 0, `${label} must be greater than 0.`)

/**
 * One schema drives both add and edit. Edit ignores `ticker`/`assetType` (the asset isn't
 * editable — delete + re-add), so the form renders them read-only / hidden in edit mode but
 * they stay valid because they're pre-filled.
 */
export const transactionFormSchema = z.object({
  ticker: z
    .string()
    .trim()
    .min(1, 'Ticker is required.')
    .max(20, 'Ticker must not exceed 20 characters.'),
  type: z.enum(['Buy', 'Sell']),
  quantity: positiveAmount('Quantity'),
  pricePerUnit: positiveAmount('Price per unit'),
  currency: z
    .string()
    .trim()
    .regex(/^[A-Za-z]{3}$/, 'Use a 3-letter ISO currency code, e.g. USD.'),
  transactionDate: z
    .string()
    .min(1, 'Transaction date is required.')
    .refine((value) => value <= todayIso(), 'Transaction date must not be in the future.'),
  assetType: z.enum(['Stock', 'Etf']),
})

export type TransactionFormValues = z.infer<typeof transactionFormSchema>

/** The form's field names, for binding a 400's PascalCase error keys back to fields. */
export const transactionFormFields = [
  'ticker',
  'type',
  'quantity',
  'pricePerUnit',
  'currency',
  'transactionDate',
  'assetType',
] as const satisfies readonly (keyof TransactionFormValues)[]
