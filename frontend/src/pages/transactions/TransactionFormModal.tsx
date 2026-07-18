import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { normalizeError, type TransactionDto } from '../../api'
import { applyApiError } from '../auth/apiFormErrors'
import styles from './TransactionsList.module.css'
import {
  transactionFormFields,
  transactionFormSchema,
  todayIso,
  type TransactionFormValues,
} from './schemas'
import { useCreateTransaction, useUpdateTransaction } from './useTransactionMutations'

interface TransactionFormModalProps {
  /** Present → edit that transaction; absent → create a new one. */
  transaction?: TransactionDto
  onClose: () => void
}

/**
 * Add/edit dialog (FR-05, FR-06). Client rules mirror the backend validators; on submit the
 * values go to POST/PUT and, on success, the mutation invalidates the list and the modal
 * closes. A 400 binds each error to its field; a 422 over-sell (or any other failure) shows
 * at form level. In edit mode the ticker is read-only and asset type is hidden — the asset
 * isn't editable (delete + re-add).
 */
export function TransactionFormModal({ transaction, onClose }: TransactionFormModalProps) {
  const isEdit = transaction !== undefined
  const [formError, setFormError] = useState<string | null>(null)
  const createMutation = useCreateTransaction()
  const updateMutation = useUpdateTransaction()

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<TransactionFormValues>({
    resolver: zodResolver(transactionFormSchema),
    defaultValues: transaction
      ? {
          ticker: transaction.ticker,
          type: transaction.type,
          quantity: String(transaction.quantity),
          pricePerUnit: String(transaction.pricePerUnit),
          currency: transaction.currency,
          transactionDate: transaction.transactionDate,
          assetType: 'Stock',
        }
      : {
          ticker: '',
          type: 'Buy',
          quantity: '',
          pricePerUnit: '',
          currency: 'USD',
          transactionDate: todayIso(),
          assetType: 'Stock',
        },
  })

  // Close on Escape, matching the click-the-backdrop affordance.
  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [onClose])

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null)
    try {
      if (transaction) {
        await updateMutation.mutateAsync({
          id: transaction.id,
          body: {
            type: values.type,
            quantity: Number(values.quantity),
            pricePerUnit: Number(values.pricePerUnit),
            currency: values.currency.toUpperCase(),
            transactionDate: values.transactionDate,
          },
        })
      } else {
        await createMutation.mutateAsync({
          ticker: values.ticker.toUpperCase(),
          type: values.type,
          quantity: Number(values.quantity),
          pricePerUnit: Number(values.pricePerUnit),
          currency: values.currency.toUpperCase(),
          transactionDate: values.transactionDate,
          assetType: values.assetType,
        })
      }
      onClose()
    } catch (error) {
      const message = applyApiError(normalizeError(error), transactionFormFields, setError)
      if (message !== null) setFormError(message)
    }
  })

  return (
    <div className={styles.backdrop} onClick={onClose} role="presentation">
      <div
        className={styles.modal}
        role="dialog"
        aria-modal="true"
        aria-labelledby="tx-modal-title"
        onClick={(e) => e.stopPropagation()}
      >
        <h2 id="tx-modal-title" className={styles.modalTitle}>
          {isEdit ? 'Edit transaction' : 'Add transaction'}
        </h2>

        <form className={styles.form} onSubmit={onSubmit} noValidate>
          {formError !== null && (
            <p className={styles.formError} role="alert">
              {formError}
            </p>
          )}

          <div className={styles.field}>
            <label className={styles.label} htmlFor="tx-ticker">
              Ticker
            </label>
            <input
              id="tx-ticker"
              className={`${styles.control} ${errors.ticker ? styles.controlError : ''}`}
              placeholder="e.g. AAPL"
              readOnly={isEdit}
              aria-invalid={errors.ticker ? true : undefined}
              {...register('ticker')}
            />
            {isEdit && (
              <span className={styles.hint}>
                Ticker can't be changed — delete and re-add instead.
              </span>
            )}
            {errors.ticker && <span className={styles.fieldError}>{errors.ticker.message}</span>}
          </div>

          <div className={styles.formRow}>
            <div className={styles.field}>
              <label className={styles.label} htmlFor="tx-type">
                Type
              </label>
              <select
                id="tx-type"
                className={styles.control}
                aria-invalid={errors.type ? true : undefined}
                {...register('type')}
              >
                <option value="Buy">Buy</option>
                <option value="Sell">Sell</option>
              </select>
              {errors.type && <span className={styles.fieldError}>{errors.type.message}</span>}
            </div>

            {!isEdit && (
              <div className={styles.field}>
                <label className={styles.label} htmlFor="tx-asset-type">
                  Asset type
                </label>
                <select id="tx-asset-type" className={styles.control} {...register('assetType')}>
                  <option value="Stock">Stock</option>
                  <option value="Etf">ETF</option>
                </select>
              </div>
            )}
          </div>

          <div className={styles.formRow}>
            <div className={styles.field}>
              <label className={styles.label} htmlFor="tx-quantity">
                Quantity
              </label>
              <input
                id="tx-quantity"
                type="number"
                step="any"
                min="0"
                inputMode="decimal"
                className={`${styles.control} ${errors.quantity ? styles.controlError : ''}`}
                aria-invalid={errors.quantity ? true : undefined}
                {...register('quantity')}
              />
              {errors.quantity && (
                <span className={styles.fieldError}>{errors.quantity.message}</span>
              )}
            </div>

            <div className={styles.field}>
              <label className={styles.label} htmlFor="tx-price">
                Price / unit
              </label>
              <input
                id="tx-price"
                type="number"
                step="any"
                min="0"
                inputMode="decimal"
                className={`${styles.control} ${errors.pricePerUnit ? styles.controlError : ''}`}
                aria-invalid={errors.pricePerUnit ? true : undefined}
                {...register('pricePerUnit')}
              />
              {errors.pricePerUnit && (
                <span className={styles.fieldError}>{errors.pricePerUnit.message}</span>
              )}
            </div>
          </div>

          <div className={styles.formRow}>
            <div className={styles.field}>
              <label className={styles.label} htmlFor="tx-currency">
                Currency
              </label>
              <input
                id="tx-currency"
                maxLength={3}
                autoCapitalize="characters"
                className={`${styles.control} ${styles.currencyInput} ${errors.currency ? styles.controlError : ''}`}
                aria-invalid={errors.currency ? true : undefined}
                {...register('currency')}
              />
              {errors.currency && (
                <span className={styles.fieldError}>{errors.currency.message}</span>
              )}
            </div>

            <div className={styles.field}>
              <label className={styles.label} htmlFor="tx-date">
                Date
              </label>
              <input
                id="tx-date"
                type="date"
                max={todayIso()}
                className={`${styles.control} ${errors.transactionDate ? styles.controlError : ''}`}
                aria-invalid={errors.transactionDate ? true : undefined}
                {...register('transactionDate')}
              />
              {errors.transactionDate && (
                <span className={styles.fieldError}>{errors.transactionDate.message}</span>
              )}
            </div>
          </div>

          <div className={styles.modalActions}>
            <button type="button" className={styles.secondaryButton} onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className={styles.primaryButton} disabled={isSubmitting}>
              {isSubmitting ? 'Saving…' : isEdit ? 'Save changes' : 'Add transaction'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
