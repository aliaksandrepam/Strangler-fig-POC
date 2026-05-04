import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Mounted under `/projects/new/` by the YARP gateway. Vite needs the matching
// base path so its asset URLs resolve correctly when accessed via the gateway.
export default defineConfig({
  base: '/projects/new/',
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    // Allow the gateway origin (and the direct origin during dev).
    cors: true,
    // HMR uses the same port; gateway proxies WS too.
    hmr: {
      // When accessed via the gateway, HMR websocket should hit the gateway.
      // Vite default is to use the page's host — the gateway proxies it on the same port.
      clientPort: 8080,
    },
  },
})
