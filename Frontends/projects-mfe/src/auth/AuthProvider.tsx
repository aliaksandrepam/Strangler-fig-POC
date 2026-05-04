import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from 'react'
import api, { clearBearerToken, setBearerToken } from '../api/client'

interface AuthUser {
  id: string
  email: string | null
  fullName: string | null
}

interface AuthState {
  status: 'loading' | 'unauthenticated' | 'authenticated'
  user: AuthUser | null
  /** Validate email + password against Auth.Api. Used by the SPA's own login form. */
  login: (email: string, password: string) => Promise<{ ok: true } | { ok: false; error: string }>
  /** Discard the in-memory JWT AND clear the MVC Identity cookie. */
  logout: () => Promise<void>
  /** Re-mint a JWT using the still-valid one. */
  refresh: () => Promise<void>
}

interface TokenResponse {
  accessToken: string
  expiresAt: string
  user: AuthUser
}

const AuthContext = createContext<AuthState | null>(null)
const STORAGE_KEY = 'pocapp.jwt'

/**
 * SSO-aware auth flow:
 *
 * 1. On mount, try POST /api/auth/exchange — sends the MVC Identity cookie
 *    (if present) and gets back a JWT. This is the "MVC first → React" path.
 * 2. If the cookie isn't present (401), fall back to sessionStorage. This is
 *    "user already used the SPA recently".
 * 3. If neither works, mark unauthenticated. RequireAuth will redirect the
 *    user to MVC's /Identity/Account/Login with a ReturnUrl back to the SPA.
 *    After they log in there, they bounce back, the cookie is set, the
 *    exchange succeeds, and they're signed in.
 *
 * The login(email, password) function is still available for the SPA's own
 * login form, but it's no longer the primary path.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthState['status']>('loading')
  const [user, setUser] = useState<AuthUser | null>(null)
  const refreshTimer = useRef<number | undefined>(undefined)

  const applyToken = useCallback((data: TokenResponse) => {
    setBearerToken(data.accessToken, data.expiresAt)
    setUser(data.user)
    setStatus('authenticated')
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(data))

    const ms = new Date(data.expiresAt).getTime() - Date.now() - 60_000
    if (refreshTimer.current) window.clearTimeout(refreshTimer.current)
    if (ms > 0) {
      refreshTimer.current = window.setTimeout(() => void refresh(), ms)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const clear = useCallback(() => {
    clearBearerToken()
    setUser(null)
    setStatus('unauthenticated')
    sessionStorage.removeItem(STORAGE_KEY)
    if (refreshTimer.current) window.clearTimeout(refreshTimer.current)
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    try {
      const { data } = await api.post<TokenResponse>('/api/auth/login', { email, password })
      applyToken(data)
      return { ok: true } as const
    } catch {
      return { ok: false, error: 'Invalid email or password.' } as const
    }
  }, [applyToken])

  const refresh = useCallback(async () => {
    try {
      const { data } = await api.post<TokenResponse>('/api/auth/refresh')
      applyToken(data)
    } catch {
      // refresh failed — try the cookie path before giving up
      try {
        const { data } = await api.post<TokenResponse>('/api/auth/exchange')
        applyToken(data)
      } catch {
        clear()
      }
    }
  }, [applyToken, clear])

  const logout = useCallback(async () => {
    try { await api.post('/api/auth/logout') } catch { /* idempotent */ }
    clear()
  }, [clear])

  // Initial bootstrap: try cookie exchange → sessionStorage → unauthenticated.
  useEffect(() => {
    let cancelled = false

    const bootstrap = async () => {
      // 1. Try to exchange an existing MVC Identity cookie for a JWT.
      try {
        const { data } = await api.post<TokenResponse>('/api/auth/exchange')
        if (!cancelled) applyToken(data)
        return
      } catch {
        // not signed in via MVC — fall through
      }

      // 2. Fall back to a JWT we may have stashed earlier in this session.
      const saved = sessionStorage.getItem(STORAGE_KEY)
      if (saved) {
        try {
          const parsed = JSON.parse(saved) as TokenResponse
          if (new Date(parsed.expiresAt).getTime() > Date.now() + 5_000) {
            if (!cancelled) applyToken(parsed)
            return
          }
        } catch { /* ignore parse errors */ }
      }

      // 3. Truly anonymous — let RequireAuth bounce to login.
      if (!cancelled) setStatus('unauthenticated')
    }

    void bootstrap()
    const onExpired = () => clear()
    window.addEventListener('pocapp:auth-expired', onExpired)
    return () => {
      cancelled = true
      window.removeEventListener('pocapp:auth-expired', onExpired)
      if (refreshTimer.current) window.clearTimeout(refreshTimer.current)
    }
  }, [applyToken, clear])

  return (
    <AuthContext.Provider value={{ status, user, login, logout, refresh }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
