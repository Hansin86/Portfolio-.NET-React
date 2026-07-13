import { beforeEach, describe, expect, it } from 'vitest'
import type { AuthResponse } from '../api'
import { clearSession, loadSession, saveSession } from './session'

const auth: AuthResponse = {
  userId: 'user-1',
  email: 'ada@example.com',
  token: 'jwt-token',
}

describe('session storage', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('returns null when nothing is stored', () => {
    expect(loadSession()).toBeNull()
  })

  it('round-trips a saved session, dropping the token from the user identity', () => {
    const saved = saveSession(auth)
    expect(saved).toEqual({
      token: 'jwt-token',
      user: { userId: 'user-1', email: 'ada@example.com' },
    })
    expect(loadSession()).toEqual(saved)
  })

  it('clears the stored session', () => {
    saveSession(auth)
    clearSession()
    expect(loadSession()).toBeNull()
  })

  it('discards corrupt JSON', () => {
    localStorage.setItem('portfolio.auth', '{not json')
    expect(loadSession()).toBeNull()
    expect(localStorage.getItem('portfolio.auth')).toBeNull()
  })

  it('discards a session with the wrong shape', () => {
    localStorage.setItem('portfolio.auth', JSON.stringify({ token: 123, user: {} }))
    expect(loadSession()).toBeNull()
  })
})
