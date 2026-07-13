import { Link } from 'react-router-dom'
import { paths } from '../routes/paths'

/** Placeholder. The real registration form lands with the Auth screens commit. */
export function RegisterPage() {
  return (
    <main style={{ maxWidth: 360, margin: '4rem auto', padding: '0 1rem' }}>
      <h1>Register</h1>
      <p>Registration form arrives in the next commit.</p>
      <p style={{ marginTop: '1.5rem' }}>
        Already have an account? <Link to={paths.login}>Log in</Link>
      </p>
    </main>
  )
}
