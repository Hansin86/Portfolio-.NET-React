import { Link, Outlet } from 'react-router-dom'
import { useAuth } from '../auth'
import { paths } from './paths'
import styles from './AppLayout.module.css'

/** Chrome for the authenticated area: header with identity + logout, then the routed page. */
export function AppLayout() {
  const { user, logout } = useAuth()

  return (
    <div className={styles.shell}>
      <header className={styles.header}>
        <Link to={paths.transactions} className={styles.brand}>
          Portfolio
        </Link>
        <nav className={styles.nav}>
          <Link to={paths.transactions}>Transactions</Link>
        </nav>
        <div className={styles.session}>
          {user && <span className={styles.email}>{user.email}</span>}
          <button type="button" onClick={logout}>
            Log out
          </button>
        </div>
      </header>
      <main className={styles.main}>
        <Outlet />
      </main>
    </div>
  )
}
