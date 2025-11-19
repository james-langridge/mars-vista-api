# Mars Vista Admin Dashboard

Admin monitoring dashboard for the Mars Vista API.

## Environment Variables

Create a `.env` file in the root of this directory with the following variables:

```bash
# API Configuration
VITE_API_URL=https://marsvista.dev

# Admin Authentication
VITE_ADMIN_EMAIL=admin@marsvista.com
VITE_ADMIN_PASSWORD=your-secure-password-here
VITE_ADMIN_API_KEY=mv_live_your_admin_api_key_here
```

See `.env.example` for a template.

## Development

```bash
npm install
npm run dev
```

The dashboard will be available at http://localhost:5175/

## Production Deployment

Set the following environment variables in your deployment platform (Vercel/Netlify/Railway/etc.):

- `VITE_API_URL` - Production API URL (e.g., https://marsvista.dev)
- `VITE_ADMIN_EMAIL` - Admin login email
- `VITE_ADMIN_PASSWORD` - Admin login password
- `VITE_ADMIN_API_KEY` - Admin API key from the backend

**Important:** These are build-time environment variables. You must rebuild the application after changing them.

## Features

- 📊 Overview - System statistics and rate limit violations
- ⚡ Performance - Response time metrics, P95/P99, slow queries
- 🎯 Endpoints - Top endpoints, rover/camera usage
- 🚨 Errors - Error tracking by status code
- 👥 Users - User management with usage stats
- 📝 Activity - Recent API activity log

Auto-refreshes every 30 seconds.

## Tech Stack

- React 19 + Vite + TypeScript
- shadcn/ui components
- Tailwind CSS v4
