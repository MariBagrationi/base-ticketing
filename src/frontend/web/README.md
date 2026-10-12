# TixFlow web app

Next.js 16 frontend for TixFlow: event listing, fair queue, checkout, My tickets, door scanner and organizer dashboard. Buyers sign in with MetaMask; one-click Admin and Staff demo accounts are available when the API exposes them (local demo setups only).

See the [root README](../../../README.md) for the full flow, architecture and Docker setup.

```bash
npm install
npm run dev        # http://localhost:3000, expects the API on http://localhost:5222
npm run build      # production build (standalone output, used by the Dockerfile)
```

Set `NEXT_PUBLIC_API_URL` to point at a different API.
