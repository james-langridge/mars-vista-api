# Mars Vista Landing Page

Modern, clean landing page for the Mars Vista API built with Vite, React, TypeScript, and Tailwind CSS.

## Development

```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

## Deployment

Deploy to Railway:

1. Create a new Railway service from this directory
2. Set root directory to `web/landing`
3. Railway will automatically detect Vite and build the site

The site will be available at `marsvista.dev`.

## Tech Stack

- **Vite** - Fast build tool and dev server
- **React 18** - UI library
- **TypeScript** - Type safety
- **Tailwind CSS v4** - Utility-first CSS (using @tailwindcss/vite plugin)
- **ESLint** - Code linting

## Project Structure

```
web/landing/
├── src/
│   ├── components/     # React components
│   ├── lib/           # Utility functions
│   ├── assets/        # Static assets
│   ├── App.tsx        # Main app component
│   ├── main.tsx       # Entry point
│   └── index.css      # Tailwind imports
├── public/            # Public static files
├── index.html         # HTML template
└── vite.config.ts     # Vite configuration
```
