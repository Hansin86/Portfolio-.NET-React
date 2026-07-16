import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router-dom'
import { authApi, normalizeError } from '../api'
import { useAuth } from '../auth'
import { paths } from '../routes/paths'
import styles from './auth/AuthForm.module.css'
import { applyApiError } from './auth/apiFormErrors'
import { registerSchema, type RegisterFormValues } from './auth/schemas'

/**
 * Real registration form. The Zod schema mirrors the backend password policy (12–128
 * chars, upper/lower/digit, no whitespace) for instant feedback; the server remains
 * the source of truth. On success the API returns a session, which useAuth().login
 * stores — the RedirectIfAuthenticated guard then lands the new user in the app. A
 * 409 (email already in use) surfaces at form level; 400 field errors bind to fields.
 */
export function RegisterPage() {
  const { login } = useAuth()
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({ resolver: zodResolver(registerSchema) })

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null)
    try {
      const auth = await authApi.register(values)
      login(auth)
    } catch (error) {
      const message = applyApiError(normalizeError(error), ['email', 'password'], setError)
      if (message !== null) setFormError(message)
    }
  })

  return (
    <div className={styles.page}>
      <div className={styles.card}>
        <h1 className={styles.title}>Create account</h1>
        <p className={styles.subtitle}>Start tracking your portfolio in a minute.</p>

        <form className={styles.form} onSubmit={onSubmit} noValidate>
          {formError !== null && (
            <p className={styles.formError} role="alert">
              {formError}
            </p>
          )}

          <div className={styles.field}>
            <label className={styles.label} htmlFor="email">
              Email
            </label>
            <input
              id="email"
              type="email"
              autoComplete="email"
              className={`${styles.input} ${errors.email ? styles.inputError : ''}`}
              aria-invalid={errors.email ? true : undefined}
              {...register('email')}
            />
            {errors.email && <span className={styles.fieldError}>{errors.email.message}</span>}
          </div>

          <div className={styles.field}>
            <label className={styles.label} htmlFor="password">
              Password
            </label>
            <input
              id="password"
              type="password"
              autoComplete="new-password"
              className={`${styles.input} ${errors.password ? styles.inputError : ''}`}
              aria-invalid={errors.password ? true : undefined}
              {...register('password')}
            />
            {errors.password ? (
              <span className={styles.fieldError}>{errors.password.message}</span>
            ) : (
              <span className={styles.fieldError} style={{ color: 'var(--text)' }}>
                At least 12 characters, with an uppercase, lowercase, and digit.
              </span>
            )}
          </div>

          <button type="submit" className={styles.submit} disabled={isSubmitting}>
            {isSubmitting ? 'Creating account…' : 'Create account'}
          </button>
        </form>

        <p className={styles.footer}>
          Already have an account? <Link to={paths.login}>Log in</Link>
        </p>
      </div>
    </div>
  )
}
