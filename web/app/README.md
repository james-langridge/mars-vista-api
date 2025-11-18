# Mars Vista Web Application

Next.js frontend application for Mars Vista API, featuring landing page, documentation, pricing, and user dashboard with Auth.js authentication.

## Tech Stack

- **Framework**: Next.js 16 with App Router
- **Language**: TypeScript
- **Styling**: Tailwind CSS
- **Authentication**: Auth.js (NextAuth v5) with magic link email
- **Database**: Prisma ORM (PostgreSQL)
- **Email**: Resend
- **Documentation**: Redoc

## Prerequisites

- Node.js 20+
- PostgreSQL database (shared with C# API)
- Resend API key for magic link emails

## Getting Started

### 1. Install Dependencies

```bash
npm install
```

### 2. Configure Environment Variables

Create `.env.local` file:

```bash
# Database (same as C# API uses)
DATABASE_URL="postgresql://marsvista:marsvista_dev_password@localhost:5432/marsvista_dev"

# Resend
RESEND_API_KEY=re_xxxxx
FROM_EMAIL=noreply@marsvista.dev

# Auth.js
AUTH_SECRET=<generate-with-openssl-rand-base64-32>
NEXTAUTH_URL=http://localhost:3000

# API Backend
NEXT_PUBLIC_API_URL=http://localhost:5127
```

Generate `AUTH_SECRET`:
```bash
openssl rand -base64 32
```

### 3. Set Up Database

The Auth.js tables were manually created via SQL (to avoid conflicts with EF Core tables):

```bash
# Tables already exist if you've run the setup
# User, Session, VerificationToken
```

Generate Prisma client:
```bash
npm run db:generate
```

### 4. Run Development Server

```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000)

### 5. Build for Production

```bash
npm run build
npm start
```

## Project Structure

```
app/
├── app/                      # Next.js App Router
│   ├── layout.tsx           # Root layout (Header, Footer)
│   ├── page.tsx             # Landing page
│   ├── api/
│   │   └── auth/[...nextauth]/route.ts  # Auth.js API routes
│   ├── docs/
│   │   └── page.tsx         # API documentation (Redoc)
│   ├── pricing/
│   │   └── page.tsx         # Pricing tiers
│   ├── signin/
│   │   └── page.tsx         # Magic link sign-in
│   ├── dashboard/
│   │   ├── layout.tsx       # Dashboard layout (sidebar)
│   │   └── page.tsx         # Dashboard home (protected)
│   ├── loading.tsx          # Loading state
│   └── error.tsx            # Error boundary
├── components/
│   ├── Header.tsx           # Navigation (with auth state)
│   ├── Footer.tsx           # Footer links
│   ├── Hero.tsx             # Landing page hero
│   ├── Features.tsx         # Features grid
│   ├── QuickStart.tsx       # Code examples
│   ├── SignInForm.tsx       # Magic link form (client component)
│   ├── SignOutButton.tsx    # Sign out button (client component)
│   ├── RedocWrapper.tsx     # Redoc integration (client component)
│   └── emails/
│       └── MagicLinkEmail.tsx  # Email template
├── server/
│   ├── db.ts                # Prisma client
│   ├── auth.ts              # Auth.js configuration
│   └── auth-export.ts       # Auth.js exports
├── prisma/
│   └── schema.prisma        # Prisma schema (Auth.js tables)
├── middleware.ts            # Auth middleware (protects /dashboard)
└── .env.local               # Environment variables (gitignored)
```

## Pages

- `/` - Landing page with hero, features, and quick start
- `/docs` - API documentation (Redoc)
- `/pricing` - Pricing tiers (Free, Pro, Enterprise)
- `/signin` - Magic link email authentication
- `/dashboard` - User dashboard (protected route)

## Authentication

Uses Auth.js (NextAuth v5) with Resend magic link provider:

1. User enters email on `/signin`
2. Magic link sent via Resend
3. User clicks link in email
4. Authenticated and redirected to `/dashboard`
5. Session managed by Auth.js with database sessions

## Database

Shares PostgreSQL database with C# API:

**Auth.js Tables** (managed by Prisma):
- `User` - User accounts
- `Session` - Active sessions
- `VerificationToken` - Magic link tokens

**API Tables** (managed by EF Core):
- `rovers` - Mars rover data
- `cameras` - Camera data
- `photos` - Photo data

## Development Notes

- Auth middleware protects `/dashboard` routes
- Header shows "Dashboard" or "Sign In" based on auth state
- Sign-out button in dashboard sidebar
- Redoc loads API spec from `NEXT_PUBLIC_API_URL/openapi.json`
- Email templates use React Email components
- Loading states and error boundaries for better UX

## Deployment

See Story 009 for Railway deployment instructions (Phase 5).

## Next Steps

- **Story 010**: Implement API key authentication backend (C# API)
- **Story 011**: Add API key management UI to dashboard
- Add usage statistics and analytics
- Implement billing integration for Pro tier
