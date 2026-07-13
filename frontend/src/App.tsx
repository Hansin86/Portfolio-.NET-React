import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { TransactionsPage } from './pages/TransactionsPage'
import { AppLayout } from './routes/AppLayout'
import { RedirectIfAuthenticated, RequireAuth } from './routes/guards'
import { HOME_PATH, paths } from './routes/paths'

function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Public: bounce to the app if already signed in. */}
        <Route element={<RedirectIfAuthenticated />}>
          <Route path={paths.login} element={<LoginPage />} />
          <Route path={paths.register} element={<RegisterPage />} />
        </Route>

        {/* Protected: guard → authenticated shell → routed pages. */}
        <Route element={<RequireAuth />}>
          <Route element={<AppLayout />}>
            <Route path="/" element={<Navigate to={HOME_PATH} replace />} />
            <Route path={paths.transactions} element={<TransactionsPage />} />
          </Route>
        </Route>

        {/* Unknown paths: send home, where the guard decides login vs. app. */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
