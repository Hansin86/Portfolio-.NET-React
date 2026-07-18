import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { seedTransactions, server } from '../test/server'
import { makeTransaction, renderWithClient } from '../test/utils'
import { TransactionsPage } from './TransactionsPage'

const BASE = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:5029'

describe('TransactionsPage', () => {
  it('renders the seeded transactions', async () => {
    seedTransactions([makeTransaction({ ticker: 'AAPL' }), makeTransaction({ ticker: 'TSLA' })])
    renderWithClient(<TransactionsPage />)

    expect(await screen.findByText('AAPL')).toBeInTheDocument()
    expect(screen.getByText('TSLA')).toBeInTheDocument()
  })

  it('shows the empty state when there are no transactions', async () => {
    seedTransactions([])
    renderWithClient(<TransactionsPage />)

    expect(await screen.findByText(/No transactions yet/i)).toBeInTheDocument()
  })

  it('adds a transaction through the modal and shows it in the list', async () => {
    const user = userEvent.setup()
    seedTransactions([])
    renderWithClient(<TransactionsPage />)
    await screen.findByText(/No transactions yet/i)

    await user.click(screen.getByRole('button', { name: 'Add transaction' }))
    const dialog = within(screen.getByRole('dialog'))
    await user.type(dialog.getByLabelText('Ticker'), 'TSLA')
    await user.type(dialog.getByLabelText('Quantity'), '5')
    await user.type(dialog.getByLabelText('Price / unit'), '100')
    await user.click(dialog.getByRole('button', { name: 'Add transaction' }))

    // POST succeeds → list invalidates and refetches → the new row appears, modal closes.
    expect(await screen.findByText('TSLA')).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('surfaces a 422 over-sell error at form level and keeps the modal open', async () => {
    const user = userEvent.setup()
    seedTransactions([])
    renderWithClient(<TransactionsPage />)
    await screen.findByText(/No transactions yet/i)

    server.use(
      http.post(`${BASE}/transactions`, () =>
        HttpResponse.json(
          {
            status: 422,
            title: 'Unprocessable Entity',
            detail: 'Cannot sell more than currently held.',
          },
          { status: 422 },
        ),
      ),
    )

    await user.click(screen.getByRole('button', { name: 'Add transaction' }))
    const dialog = within(screen.getByRole('dialog'))
    await user.type(dialog.getByLabelText('Ticker'), 'TSLA')
    await user.selectOptions(dialog.getByLabelText('Type'), 'Sell')
    await user.type(dialog.getByLabelText('Quantity'), '5')
    await user.type(dialog.getByLabelText('Price / unit'), '100')
    await user.click(dialog.getByRole('button', { name: 'Add transaction' }))

    expect(await screen.findByText(/Cannot sell more than currently held/i)).toBeInTheDocument()
    expect(screen.getByRole('dialog')).toBeInTheDocument()
  })

  it('deletes a transaction after confirmation', async () => {
    const user = userEvent.setup()
    seedTransactions([makeTransaction({ ticker: 'AAPL' })])
    renderWithClient(<TransactionsPage />)
    await screen.findByText('AAPL')

    await user.click(screen.getByRole('button', { name: 'Delete' }))
    const dialog = within(screen.getByRole('alertdialog'))
    await user.click(dialog.getByRole('button', { name: 'Delete' }))

    await waitFor(() => expect(screen.queryByText('AAPL')).not.toBeInTheDocument())
  })
})
