import { apiClient } from './client'
import type { LoginRequest, RegisterRequest } from './requests'
import type { AuthResponse } from './types'

/** POST /auth/register — create an account and return a session token. */
export async function register(body: RegisterRequest): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>('/auth/register', body)
  return data
}

/** POST /auth/login — exchange credentials for a session token. */
export async function login(body: LoginRequest): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>('/auth/login', body)
  return data
}
