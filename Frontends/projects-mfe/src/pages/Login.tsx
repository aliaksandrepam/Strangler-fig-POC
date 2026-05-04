import { useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthProvider'

/**
 * SPA login form for the Auth.Api flow. Posts credentials to /api/auth/login
 * (proxied to Auth.Api by the gateway) and stores the resulting JWT.
 *
 * Note: this is a SEPARATE login from the legacy MVC /Identity/Account/Login
 * page. The two flows validate against the same AspNetUsers table but issue
 * different credentials (cookie vs JWT). That's the documented Option-2 trade-off.
 */
export default function Login() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from ?? '/'

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    const result = await login(email, password)
    setSubmitting(false)
    if (result.ok) navigate(from, { replace: true })
    else setError(result.error)
  }

  return (
    <div className="row justify-content-center">
      <div className="col-md-5">
        <div className="card mt-5">
          <div className="card-body p-4">
            <h2 className="mb-3">Sign in</h2>
            <p className="text-muted small">
              This is the React SPA's own login form (Auth.Api). The legacy MVC
              app at <a href="/Identity/Account/Login">/Identity/Account/Login</a>{' '}
              has a separate flow.
            </p>

            <form onSubmit={onSubmit}>
              {error && <div className="alert alert-danger py-2">{error}</div>}

              <div className="mb-3">
                <label className="form-label">Email</label>
                <input
                  type="email"
                  className="form-control"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  autoFocus
                  autoComplete="username"
                />
              </div>

              <div className="mb-3">
                <label className="form-label">Password</label>
                <input
                  type="password"
                  className="form-control"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  autoComplete="current-password"
                />
              </div>

              <button type="submit" className="btn btn-primary w-100" disabled={submitting}>
                {submitting ? 'Signing in…' : 'Sign in'}
              </button>
            </form>

            <hr className="my-3" />
            <p className="small text-muted mb-0">
              Demo accounts:<br />
              <code>alice@example.com / Passw0rd!</code><br />
              <code>bob@example.com / Passw0rd!</code>
            </p>
            <p className="small text-muted mt-2 mb-0">
              <Link to="/">Cancel</Link> &middot;{' '}
              <a href="/">Back to legacy app</a>
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
