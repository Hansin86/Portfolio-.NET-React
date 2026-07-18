import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterAll, afterEach, beforeAll } from 'vitest'
import { resetDb, server } from './server'

// Global test setup: jest-dom matchers, and the MSW lifecycle. `onUnhandledRequest: 'error'`
// makes any request that isn't mocked fail loudly — usually a sign the base URL drifted.
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
afterEach(() => {
  cleanup()
  server.resetHandlers()
  resetDb()
})
afterAll(() => server.close())
