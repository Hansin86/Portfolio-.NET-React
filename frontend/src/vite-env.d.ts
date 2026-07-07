/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Base URL of the PortfolioApp API, e.g. http://localhost:5029. Inlined at build time. */
  readonly VITE_API_BASE_URL: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
