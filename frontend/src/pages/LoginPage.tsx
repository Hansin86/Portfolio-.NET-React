import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router-dom'
import { authApi, normalizeError } from '../api'
import { useAuth } from '../auth'
import { paths } from '../routes/paths'
import styles from './auth/AuthForm.module.css'
import { applyApiError } from './auth/apiFormErrors'
import { loginSchema, type LoginFormValues } from './auth/schemas'

/**
 * Real login form. Client-side rules mirror the backend (presence only for login);
 * on submit, credentials go to POST /auth/login and a success stores the session
 * via useAuth().login — the RedirectIfAuthenticated guard then sends the user on to
 * where they were headed. A 401 (bad credentials) is shown at form level so we don't
 * reveal which field was wrong; a 400 binds to its field.
 */
export function LoginPage() {
  const { login } = useAuth()
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({ resolver: zodResolver(loginSchema) })

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null)
    try {
      const auth = await authApi.login(values)
      login(auth)
    } catch (error) {
      const message = applyApiError(normalizeError(error), ['email', 'password'], setError)
      if (message !== null) setFormError(message)
    }
  })

  return (
    <div className={styles.page}>
      <div className={styles.card}>
        <h1 className={styles.title}>Log in</h1>
        <p className={styles.subtitle}>Welcome back — sign in to your portfolio.</p>

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
              autoComplete="current-password"
              className={`${styles.input} ${errors.password ? styles.inputError : ''}`}
              aria-invalid={errors.password ? true : undefined}
              {...register('password')}
            />
            {errors.password && (
              <span className={styles.fieldError}>{errors.password.message}</span>
            )}
          </div>

          <button type="submit" className={styles.submit} disabled={isSubmitting}>
            {isSubmitting ? 'Signing in…' : 'Log in'}
          </button>
        </form>

        <p className={styles.footer}>
          No account? <Link to={paths.register}>Register</Link>
        </p>
      </div>
    </div>
  )
}
