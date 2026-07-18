import { useEffect, useState } from 'react'
import { normalizeError, type TransactionDto } from '../../api'
import styles from './TransactionsList.module.css'
import { useDeleteTransaction } from './useTransactionMutations'

interface DeleteTransactionDialogProps {
  transaction: TransactionDto
  onClose: () => void
}

/**
 * Confirm-then-delete dialog (FR-06). On confirm the mutation invalidates the list and the
 * dialog closes. A 422 (deleting a buy that would leave a negative holding) or any other
 * failure is shown inline so the row stays put.
 */
export function DeleteTransactionDialog({ transaction, onClose }: DeleteTransactionDialogProps) {
  const deleteMutation = useDeleteTransaction()
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [onClose])

  async function confirmDelete() {
    setError(null)
    try {
      await deleteMutation.mutateAsync(transaction.id)
      onClose()
    } catch (err) {
      setError(normalizeError(err).message)
    }
  }

  return (
    <div className={styles.backdrop} onClick={onClose} role="presentation">
      <div
        className={styles.modal}
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="tx-delete-title"
        onClick={(e) => e.stopPropagation()}
      >
        <h2 id="tx-delete-title" className={styles.modalTitle}>
          Delete transaction?
        </h2>
        <p className={styles.confirmText}>
          {transaction.type} {transaction.ticker} · {transaction.quantity} @{' '}
          {transaction.pricePerUnit} {transaction.currency} on {transaction.transactionDate}. This
          can't be undone.
        </p>

        {error !== null && (
          <p className={styles.formError} role="alert">
            {error}
          </p>
        )}

        <div className={styles.modalActions}>
          <button type="button" className={styles.secondaryButton} onClick={onClose}>
            Cancel
          </button>
          <button
            type="button"
            className={styles.dangerButton}
            onClick={confirmDelete}
            disabled={deleteMutation.isPending}
          >
            {deleteMutation.isPending ? 'Deleting…' : 'Delete'}
          </button>
        </div>
      </div>
    </div>
  )
}
