import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App.tsx'

// Inject the same stylesheets the legacy MVC layout uses so the React MFE is
// visually identical. These paths bypass Vite's base-path transform because
// they're added imperatively at runtime — Vite only rewrites <link> tags it
// finds in index.html. They're served by MVC's wwwroot via the YARP fallback.
const SHARED_ASSETS = [
  '/lib/bootstrap/dist/css/bootstrap.min.css',
  '/css/site.css',
  '/css/calendar.css',
  '/css/timer.css',
]
for (const href of SHARED_ASSETS) {
  if (!document.querySelector(`link[href="${href}"]`)) {
    const link = document.createElement('link')
    link.rel = 'stylesheet'
    link.href = href
    document.head.appendChild(link)
  }
}

// Bootstrap JS bundle (for the navbar collapse on small screens)
if (!document.querySelector('script[data-pocapp-bootstrap]')) {
  const s = document.createElement('script')
  s.src = '/lib/bootstrap/dist/js/bootstrap.bundle.min.js'
  s.async = true
  s.dataset.pocappBootstrap = 'true'
  document.body.appendChild(s)
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
