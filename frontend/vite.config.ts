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
  },
})
