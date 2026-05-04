import axios, { AxiosError } from 'axios'

// Same-origin: every request goes through the YARP gateway.
// /api/* → projects-api (or MVC for /api/auth/*)
const api = axios.create({
  baseURL: '/',
  withCredentials: true, // include the MVC Identity cookie when calling /api/auth/*
})

let bearerToken: string | null = null
let tokenExpiresAt: number = 0

export function setBearerToken(token: string, expiresAtIso: string) {
  bearerToken = token
  tokenExpiresAt = new Date(expiresAtIso).getTime()
}

export function clearBearerToken() {
  bearerToken = null
  tokenExpiresAt = 0
}

api.interceptors.request.use((config) => {
  if (bearerToken && tokenExpiresAt > Date.now() + 5_000) {
    config.headers.Authorization = `Bearer ${bearerToken}`
  }
  return config
})

api.interceptors.response.use(
  (r) => r,
  (err: AxiosError) => {
    // 401 on RESOURCE endpoints → token missing/expired → tell AuthProvider.
    // 401 on /api/auth/* is expected (no cookie, bad password, expired refresh)
    // and must not trigger the auth-expired event or we loop forever.
    const url = (err.config?.url ?? '') as string
    const isAuthEndpoint = url.startsWith('/api/auth/') || url.startsWith('api/auth/')
    if (err.response?.status === 401 && !isAuthEndpoint) {
      clearBearerToken()
      window.dispatchEvent(new CustomEvent('pocapp:auth-expired'))
    }
    return Promise.reject(err)
  }
)

export default api
