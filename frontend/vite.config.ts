/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Must match the origin allowed by the API's SpaCors policy (Cors:AllowedOrigins).
    port: 5173,
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    // Pin the API base URL so the client and the MSW handlers agree on the host.
    env: { VITE_API_BASE_URL: 'http://localhost:5029' },
  },
})
