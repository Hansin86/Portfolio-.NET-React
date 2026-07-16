import { describe, expect, it } from 'vitest'
import { loginSchema, registerSchema } from './schemas'

describe('registerSchema', () => {
  const valid = { email: 'ada@example.com', password: 'Sup3rSecret!' }

  it('accepts a policy-compliant email and password', () => {
    expect(registerSchema.safeParse(valid).success).toBe(true)
  })

  it.each([
    ['too short', 'Ab1cdef', 'at least 12'],
    ['no uppercase', 'sup3rsecretpw', 'uppercase'],
    ['no lowercase', 'SUP3RSECRETPW', 'lowercase'],
    ['no digit', 'SuperSecretPw', 'digit'],
    ['has whitespace', 'Sup3r Secret Pw', 'whitespace'],
  ])('rejects a password that is %s', (_label, password, fragment) => {
    const result = registerSchema.safeParse({ ...valid, password })
    expect(result.success).toBe(false)
    if (!result.success) {
      const messages = result.error.issues.map((issue) => issue.message).join(' ')
      expect(messages).toContain(fragment)
    }
  })

  it('rejects an invalid email', () => {
    expect(registerSchema.safeParse({ ...valid, email: 'not-an-email' }).success).toBe(false)
  })
})

describe('loginSchema', () => {
  it('accepts any non-empty password (correctness is server-side)', () => {
    expect(loginSchema.safeParse({ email: 'ada@example.com', password: 'x' }).success).toBe(true)
  })

  it('rejects an empty password', () => {
    expect(loginSchema.safeParse({ email: 'ada@example.com', password: '' }).success).toBe(false)
  })

  it('rejects an invalid email', () => {
    expect(loginSchema.safeParse({ email: 'nope', password: 'whatever' }).success).toBe(false)
  })
})
