import { BrowserRouter, Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { AuthProvider, useAuth } from './auth/AuthProvider'
import AppShell from './components/AppShell'
import ProjectsList from './pages/ProjectsList'
import ProjectDetail from './pages/ProjectDetail'
import ProjectForm from './pages/ProjectForm'

const queryClient = new QueryClient({
  defaultOptions: { queries: { staleTime: 30_000, retry: 1 } },
})

function RequireAuth({ children }: { children: ReactNode }) {
  const { status } = useAuth()
  const location = useLocation()
  if (status === 'loading') return <p className="text-muted">Authenticating…</p>
  if (status === 'unauthenticated') {
    // SSO: bounce to MVC's existing login form. ReturnUrl brings them back
    // to the SPA path they were trying to reach. After MVC sets the cookie,
    // AuthProvider's bootstrap calls /api/auth/exchange and the user lands
    // signed-in without ever seeing the SPA's own login form.
    const here = `/projects/new${location.pathname}${location.search}`
    const mvcLogin = `/Identity/Account/Login?ReturnUrl=${encodeURIComponent(here)}`
    window.location.href = mvcLogin
    return <p className="text-muted">Redirecting to login…</p>
  }
  return <>{children}</>
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter basename="/projects/new">
          <AppShell>
            <Routes>
              <Route path="/" element={<RequireAuth><ProjectsList /></RequireAuth>} />
              <Route path="/new" element={<RequireAuth><ProjectForm mode="create" /></RequireAuth>} />
              <Route path="/:id" element={<RequireAuth><ProjectDetail /></RequireAuth>} />
              <Route path="/:id/edit" element={<RequireAuth><ProjectForm mode="edit" /></RequireAuth>} />
            </Routes>
          </AppShell>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  )
}
