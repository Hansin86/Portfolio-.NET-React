import { Link } from 'react-router-dom'
import { useAuth } from '../auth'
import { paths } from '../routes/paths'

/**
 * Placeholder. The real React Hook Form + Zod login form lands in the next commit
 * (Auth screens). For now the "dev login" button below logs in with a stub session
 * so routing, the RequireAuth guard, the layout, and logout can be exercised
 * end-to-end. Once authenticated, RedirectIfAuthenticated sends the user onward.
 */
export function LoginPage() {
  const { login } = useAuth()

  function handleDevLogin() {
    login({ userId: 'dev-user', email: 'dev@example.com', token: 'dev-token' })
  }

  return (
    <main style={{ maxWidth: 360, margin: '4rem auto', padding: '0 1rem' }}>
      <h1>Log in</h1>
      <p>Login form arrives in the next commit.</p>
      <button type="button" onClick={handleDevLogin}>
        Dev login (placeholder)
      </button>
      <p style={{ marginTop: '1.5rem' }}>
        No account? <Link to={paths.register}>Register</Link>
      </p>
    </main>
  )
}
