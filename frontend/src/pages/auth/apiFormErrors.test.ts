import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api'
import { applyApiError } from './apiFormErrors'

const fields = ['email', 'password'] as const

describe('applyApiError', () => {
  it('binds 400 field errors to their (lower-cased) fields and returns null', () => {
    const setError = vi.fn()
    const error = new ApiError('validation', 400, null, {
      Email: ['Enter a valid email address.'],
      Password: ['Too short.', 'No digit.'],
    })

    const formError = applyApiError(error, fields, setError)

    expect(formError).toBeNull()
    expect(setError).toHaveBeenCalledWith('email', {
      type: 'server',
      message: 'Enter a valid email address.',
    })
    expect(setError).toHaveBeenCalledWith('password', {
      type: 'server',
      message: 'Too short. No digit.',
    })
  })

  it('returns the message for a 401 without touching fields', () => {
    const setError = vi.fn()
    const error = new ApiError('Invalid email or password.', 401, null, null)

    expect(applyApiError(error, fields, setError)).toBe('Invalid email or password.')
    expect(setError).not.toHaveBeenCalled()
  })

  it('surfaces a 409 at form level', () => {
    const setError = vi.fn()
    const error = new ApiError('Email is already in use.', 409, null, null)

    expect(applyApiError(error, fields, setError)).toBe('Email is already in use.')
    expect(setError).not.toHaveBeenCalled()
  })

  it('does not swallow a 400 whose keys match no known field', () => {
    const setError = vi.fn()
    const error = new ApiError('validation', 400, null, {
      SomethingElse: ['Off-form problem.'],
    })

    expect(applyApiError(error, fields, setError)).toBe('Off-form problem.')
    expect(setError).not.toHaveBeenCalled()
  })
})
