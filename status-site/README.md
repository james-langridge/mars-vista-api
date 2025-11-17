# Mars Vista Status Page

Real-time system status and uptime monitoring for Mars Vista API.

## Features

- **Real-time status updates** - Automatically refreshes every 60 seconds
- **Beautiful UI** - Modern, responsive design with Mars Vista branding
- **Incident logging** - View recent uptime/downtime events with timestamps
- **UptimeRobot integration** - Powered by UptimeRobot monitoring service

## Tech Stack

- **React 19** with TypeScript
- **Vite** for fast development and optimized builds
- **Tailwind CSS 4** for modern styling
- **UptimeRobot API** for status monitoring

## Local Development

### Prerequisites

- Node.js 18+
- npm or yarn

### Setup

1. Install dependencies:
   ```bash
   npm install
   ```

2. (Optional) Configure environment variables:
   ```bash
   cp .env.example .env
   # Edit .env and add your UptimeRobot monitor API key
   ```

3. Start the development server:
   ```bash
   npm run dev
   ```

4. Open http://localhost:5173

### Building for Production

```bash
npm run build
npm run preview  # Preview the production build locally
```

## Deployment to Railway

Railway will auto-detect this as a Vite project and deploy with zero configuration!

### Setup Steps

1. Create a new Railway service from the `status-site` directory (or link to GitHub repo)
2. Add environment variable in Railway dashboard:
   - `VITE_UPTIME_ROBOT_API_KEY` = your UptimeRobot monitor API key
3. Deploy!

Railway will automatically:
- Detect the Vite project
- Install dependencies with `npm install`
- Build with `npm run build`
- Serve the static files from `dist/`

### Custom Domain

After deployment, configure your custom domain (e.g., `status.marsvista.dev`) in the Railway dashboard under Settings → Domains.

## Project Structure

```
status-site/
├── src/
│   ├── components/
│   │   ├── Header.tsx         # Site header with Mars Vista branding
│   │   ├── StatusCard.tsx     # Individual service status cards
│   │   └── IncidentLog.tsx    # Recent activity log
│   ├── types.ts               # TypeScript type definitions
│   ├── App.tsx                # Main application component
│   ├── index.css              # Tailwind CSS import
│   └── main.tsx               # Application entry point
├── vite.config.ts             # Vite configuration with Tailwind plugin
└── package.json
```

## Environment Variables

- `VITE_UPTIME_ROBOT_API_KEY` - Monitor-specific API key from UptimeRobot (starts with 'm')
  - Get this from UptimeRobot dashboard → My Settings → API Settings
  - Use the monitor-specific key, not the account-level key
  - If not provided, falls back to hardcoded key (not recommended for production)

## UptimeRobot Integration

The status page fetches data from the UptimeRobot v2 API:

- **Endpoint**: `https://api.uptimerobot.com/v2/getMonitors`
- **Method**: POST
- **Parameters**:
  - `api_key`: Monitor-specific API key
  - `logs`: Include incident logs (0 or 1)
  - `logs_limit`: Number of recent logs to fetch

Monitor status codes:
- `2` = Up (operational)
- `8` = Seems down (degraded)
- `9` = Down
- `0` = Paused

## Contributing

This is part of the Mars Vista API project. See the main repository README for contribution guidelines.

## License

MIT
