import { z } from 'zod'

// Client-side mirror of the backend auth validators (RegisterCommandValidator /
// LoginCommandValidator). Messages match the server's so a field that slips past
// the client and fails server-side reads the same. The server stays the source
// of truth — this is only fast, friendly feedback.

const email = z
  .email('Enter a valid email address.')
  .max(256, 'Email must not exceed 256 characters.')

/** Full password policy — enforced on register (12–128, upper/lower/digit, no whitespace). */
const password = z
  .string()
  .min(12, 'Password must be at least 12 characters long.')
  .max(128, 'Password must not exceed 128 characters.')
  .regex(/[A-Z]/, 'Password must contain at least one uppercase letter.')
  .regex(/[a-z]/, 'Password must contain at least one lowercase letter.')
  .regex(/[0-9]/, 'Password must contain at least one digit.')
  .refine((value) => !/\s/.test(value), 'Password must not contain whitespace.')

/** Login only checks presence — the server verifies correctness (kept indistinguishable). */
export const loginSchema = z.object({
  email,
  password: z.string().min(1, 'Password is required.'),
})

export const registerSchema = z.object({
  email,
  password,
})

export type LoginFormValues = z.infer<typeof loginSchema>
export type RegisterFormValues = z.infer<typeof registerSchema>
