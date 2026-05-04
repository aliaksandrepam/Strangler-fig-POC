import type { ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthProvider'
import TimerWidget from './TimerWidget'

/**
 * Renders exactly the same DOM as PocApp/Views/Shared/_Layout.cshtml so the
 * React MFE looks pixel-identical to the legacy MVC pages. Stylesheets are
 * loaded in index.html from the gateway (Bootstrap + site.css + calendar.css).
 *
 * Cross-app links (Home, Projects legacy, Tasks, Users, Logout) are plain
 * <a> tags that trigger full-page navigation via the gateway. The "Projects
 * (new)" link stays inside the SPA via Router.
 */
export default function AppShell({ children }: { children: ReactNode }) {
  const { status, user, logout } = useAuth()
  const navigate = useNavigate()
  const signedIn = status === 'authenticated'

  const onLogout = async (e: React.MouseEvent) => {
    e.preventDefault()
    // Auth.Api's logout deletes the MVC Identity cookie too (Set-Cookie with
    // an expired date). After it returns, navigate to the legacy home page —
    // there's no longer a session anywhere.
    await logout()
    window.location.href = '/'
  }

  return (
    <>
      <header>
        <nav className="navbar navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow mb-3">
          <div className="container-fluid">
            <a className="navbar-brand" href="/">PocApp</a>
            <button
              className="navbar-toggler"
              type="button"
              data-bs-toggle="collapse"
              data-bs-target=".navbar-collapse"
              aria-controls="navbarSupportedContent"
              aria-expanded="false"
              aria-label="Toggle navigation"
            >
              <span className="navbar-toggler-icon"></span>
            </button>
            <div className="navbar-collapse collapse d-sm-inline-flex justify-content-between">
              <ul className="navbar-nav flex-grow-1">
                <li className="nav-item">
                  <a className="nav-link text-dark" href="/">Home</a>
                </li>
                <li className="nav-item">
                  <a className="nav-link text-dark" href="/Projects">Projects</a>
                </li>
                <li className="nav-item">
                  <a
                    className="nav-link text-dark active"
                    href="/projects/new/"
                    title="Same data, new React UI"
                  >
                    Projects (new){' '}
                    <span className="badge bg-info" style={{ fontSize: '0.6rem', verticalAlign: 'middle' }}>
                      React
                    </span>
                  </a>
                </li>
                <li className="nav-item">
                  <a className="nav-link text-dark" href="/Tasks">Tasks</a>
                </li>
                <li className="nav-item">
                  <a className="nav-link text-dark" href="/Users">Users</a>
                </li>
              </ul>

              {/* Live timer — mirrors PocApp/Views/Shared/_Layout.cshtml */}
              <TimerWidget />

              {/* Login partial — mirrors Views/Shared/_LoginPartial.cshtml */}
              <ul className="navbar-nav">
                {signedIn ? (
                  <>
                    <li className="nav-item">
                      <span className="nav-link text-dark" title={user?.email ?? ''}>
                        Hello {user?.email}!
                      </span>
                    </li>
                    <li className="nav-item">
                      {/* SPA-side logout: discard JWT in memory, redirect to /login */}
                      <a className="nav-link text-dark" href="#" onClick={onLogout}>
                        Logout
                      </a>
                    </li>
                  </>
                ) : (
                  <>
                    <li className="nav-item">
                      <a className="nav-link text-dark" href="/Identity/Account/Register">
                        Register
                      </a>
                    </li>
                    <li className="nav-item">
                      {/* Single sign-on: bounce to MVC's existing login form,
                          then return here. AuthProvider exchanges the cookie. */}
                      <a className="nav-link text-dark"
                         href="/Identity/Account/Login?ReturnUrl=%2Fprojects%2Fnew%2F">
                        Login
                      </a>
                    </li>
                  </>
                )}
              </ul>
            </div>
          </div>
        </nav>
      </header>

      <div className="container">
        <main role="main" className="pb-3">
          {children}
        </main>
      </div>

      <footer className="border-top footer text-muted">
        <div className="container">
          &copy; 2026 - PocApp -{' '}
          <a href="/Home/Privacy">Privacy</a>
        </div>
      </footer>
    </>
  )
}
