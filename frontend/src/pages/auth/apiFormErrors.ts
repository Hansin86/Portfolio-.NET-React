import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'
import type { ApiError } from '../../api'

/**
 * Fold an {@link ApiError} into a form. A 400 validation response binds each error
 * to its field — the server keys are PascalCase (`Email`), so they're lower-cased
 * to match the RHF field names (`email`). Every other failure (401 bad credentials,
 * 409 duplicate email, network, 5xx) is form-level: the returned string is the
 * message to render above the form. A 400 whose keys all bind to fields returns
 * null, meaning "nothing left to show at form level".
 *
 * @param fields the form's known field names; only these are bound, anything else
 *   is surfaced at form level so a server error can't go silently unshown.
 */
export function applyApiError<TFieldValues extends FieldValues>(
  error: ApiError,
  fields: readonly Path<TFieldValues>[],
  setError: UseFormSetError<TFieldValues>,
): string | null {
  if (!error.isValidation || error.validationErrors === null) {
    return error.message
  }

  let boundAny = false
  let firstUnbound: string | null = null

  for (const [key, messages] of Object.entries(error.validationErrors)) {
    const field = toFieldName(key) as Path<TFieldValues>
    const message = messages.join(' ')
    if (fields.includes(field)) {
      setError(field, { type: 'server', message })
      boundAny = true
    } else if (firstUnbound === null) {
      firstUnbound = message
    }
  }

  // If nothing landed on a field, don't swallow the error — show it at form level.
  return boundAny ? null : (firstUnbound ?? error.message)
}

/** `Email` -> `email`. Server property names are PascalCase; RHF fields are camelCase. */
function toFieldName(key: string): string {
  return key.charAt(0).toLowerCase() + key.slice(1)
}
