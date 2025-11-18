# Story 009: Unified Next.js Frontend Application

## Status
Planning

## Overview
Consolidate separate Vite apps (`web/landing/` and `web/docs-site/`) into a single Next.js application that will host landing page, documentation, pricing, and signup/verification pages. Keep `status-site/` separate as it has different deployment requirements.

## Context

**Current structure**:
- `web/landing/` - Vite + React landing page
- `web/docs-site/` - Vite + React API docs (uses Redoc)
- `status-site/` - Vite + React status page (separate deployment)

**Problems**:
1. Multiple separate deployments for related content
2. No shared components/styling between landing and docs
3. Adding new pages (pricing, signup, verify) would create more fragmentation
4. Harder to maintain consistent navigation/branding

**Solution**: Single Next.js app at `web/app/` with all user-facing pages except status.

## Goals

1. Create new Next.js app at `web/app/` with App Router
2. Set up Auth.js (NextAuth v5) for dashboard authentication with magic links
3. Migrate landing page content to Next.js
4. Migrate docs site content to Next.js (keep Redoc integration)
5. Add new pages: `/pricing`, `/signin`, `/dashboard`
6. Shared layout with consistent header/footer navigation
7. Single deployment to Railway
8. Keep `status-site/` completely separate (different domain/deployment)

## Why Next.js?

**Advantages over separate Vite apps**:
- ✅ **File-based routing**: Easy to add new pages (`/pricing`, `/signup`, etc.)
- ✅ **Shared layouts**: Common header/footer across all pages
- ✅ **SEO optimization**: Server-side rendering for landing/marketing pages
- ✅ **API routes**: Can handle form submissions, magic link verification in same app
- ✅ **Image optimization**: Built-in next/image for Mars rover photos
- ✅ **Production-ready**: Best practices out of the box
- ✅ **Easy deployment**: Railway has excellent Next.js support

**Why not stick with Vite**:
- ❌ Vite is great for SPAs, but we need multiple pages with shared layouts
- ❌ Client-side routing (React Router) worse for SEO
- ❌ Would need to build our own API route handling
- ❌ No built-in SSR/SSG for landing page SEO

## Architecture

### Directory Structure

```
web/
├── app/                    # NEW: Unified Next.js application
│   ├── app/                # Next.js App Router
│   │   ├── layout.tsx      # Root layout (header, footer)
│   │   ├── page.tsx        # Landing page (/)
│   │   ├── docs/
│   │   │   ├── page.tsx    # API docs (/docs)
│   │   │   └── layout.tsx  # Docs-specific layout
│   │   ├── pricing/
│   │   │   └── page.tsx    # Pricing page (/pricing)
│   │   ├── signin/
│   │   │   └── page.tsx    # Sign in page (/signin)
│   │   ├── dashboard/
│   │   │   ├── page.tsx    # Dashboard home (protected)
│   │   │   └── layout.tsx  # Dashboard layout
│   │   └── api/
│   │       └── auth/
│   │           └── [...nextauth]/
│   │               └── route.ts  # Auth.js API routes
│   ├── components/
│   │   ├── Header.tsx
│   │   ├── Footer.tsx
│   │   ├── Hero.tsx
│   │   ├── Features.tsx
│   │   ├── PricingTable.tsx
│   │   └── emails/
│   │       └── MagicLinkEmail.tsx  # Email template
│   ├── server/
│   │   ├── auth.ts         # Auth.js configuration
│   │   ├── auth-export.ts  # Export auth helpers
│   │   └── db.ts           # Prisma client
│   ├── prisma/
│   │   └── schema.prisma   # Auth.js database schema
│   ├── middleware.ts       # Protect /dashboard routes
│   ├── public/
│   │   └── images/
│   ├── package.json
│   └── next.config.ts
├── landing/                # DEPRECATED: Remove after migration
├── docs-site/              # DEPRECATED: Remove after migration
└── (deleted after migration)

status-site/                # KEEP SEPARATE (different domain)
├── (unchanged)
```

### Page Routes

**Main site** (`https://marsvista.dev`):
- `/` - Landing page with hero, features, CTA
- `/docs` - API documentation (Redoc integration)
- `/pricing` - Pricing tiers (Free, Pro, Enterprise)
- `/signin` - Sign in with magic link (Auth.js)
- `/dashboard` - User dashboard (protected, view/manage API keys)
- `/about` (future) - About the project
- `/blog` (future) - Updates and tutorials

**Status site** (`https://status.marsvista.dev`):
- `/` - Uptime status dashboard
- (Keep completely separate)

## Implementation Steps

### Phase 1: Create Next.js App and Auth Setup (Day 1)

1. ✅ Create new Next.js app:
   ```bash
   cd web
   npx create-next-app@latest app
   # Options:
   # - TypeScript: Yes
   # - ESLint: Yes
   # - Tailwind CSS: Yes
   # - App Router: Yes
   # - Import alias: @/*
   ```

2. ✅ Install Auth.js dependencies:
   ```bash
   cd app
   npm install next-auth@beta @auth/prisma-adapter prisma @prisma/client
   npm install resend @react-email/components @react-email/render
   npm install --save-dev @types/node
   ```

3. ✅ Set up Prisma for Auth.js:
   ```bash
   npx prisma init
   ```

   Create `prisma/schema.prisma`:
   ```prisma
   datasource db {
     provider = "postgresql"
     url      = env("DATABASE_URL")
   }

   generator client {
     provider = "prisma-client-js"
   }

   model User {
     id            String    @id @default(cuid())
     email         String?   @unique
     emailVerified DateTime?
     name          String?
     image         String?
     sessions      Session[]
     createdAt     DateTime  @default(now())
     updatedAt     DateTime  @updatedAt
   }

   model Session {
     id           String   @id @default(cuid())
     sessionToken String   @unique
     userId       String
     expires      DateTime
     user         User     @relation(fields: [userId], references: [id], onDelete: Cascade)
   }

   model VerificationToken {
     identifier String
     token      String   @unique
     expires    DateTime

     @@unique([identifier, token])
   }
   ```

4. ✅ Create Prisma client:

   Create `server/db.ts`:
   ```typescript
   import { PrismaClient } from '@prisma/client';

   const globalForPrisma = globalThis as unknown as {
     prisma: PrismaClient | undefined;
   };

   export const db =
     globalForPrisma.prisma ??
     new PrismaClient({
       log: ['query'],
     });

   if (process.env.NODE_ENV !== 'production') globalForPrisma.prisma = db;
   ```

5. ✅ Configure Auth.js:

   Create `server/auth.ts`:
   ```typescript
   import { PrismaAdapter } from '@auth/prisma-adapter';
   import { type DefaultSession, type NextAuthConfig } from 'next-auth';
   import Resend from 'next-auth/providers/resend';
   import { db } from './db';
   import { render } from '@react-email/render';
   import { MagicLinkEmail } from '@/components/emails/MagicLinkEmail';
   import { Resend as ResendClient } from 'resend';

   const resend = new ResendClient(process.env.RESEND_API_KEY);

   declare module 'next-auth' {
     interface Session extends DefaultSession {
       user: {
         id: string;
       } & DefaultSession['user'];
     }
   }

   export const authConfig = {
     trustHost: true,
     providers: [
       Resend({
         from: process.env.FROM_EMAIL || 'noreply@marsvista.dev',
         sendVerificationRequest: async ({ identifier: email, url }) => {
           try {
             const html = await render(MagicLinkEmail({ url }));

             await resend.emails.send({
               from: process.env.FROM_EMAIL || 'noreply@marsvista.dev',
               to: email,
               subject: 'Sign in to Mars Vista',
               html,
             });
           } catch (error) {
             console.error('Failed to send magic link email:', error);
             throw error;
           }
         },
       }),
     ],
     adapter: PrismaAdapter(db),
     callbacks: {
       session: ({ session, user }) => ({
         ...session,
         user: {
           ...session.user,
           id: user.id,
         },
       }),
     },
     pages: {
       signIn: '/signin',
       error: '/signin',
     },
   } satisfies NextAuthConfig;
   ```

   Create `server/auth-export.ts`:
   ```typescript
   import NextAuth from 'next-auth';
   import { authConfig } from './auth';

   export const { handlers, auth, signIn, signOut } = NextAuth(authConfig);
   ```

6. ✅ Create Auth.js API route:

   Create `app/api/auth/[...nextauth]/route.ts`:
   ```typescript
   import { handlers } from '@/server/auth-export';

   export const { GET, POST } = handlers;
   ```

7. ✅ Create magic link email template:

   Create `components/emails/MagicLinkEmail.tsx`:
   ```typescript
   import {
     Body,
     Container,
     Head,
     Heading,
     Html,
     Link,
     Preview,
     Text,
   } from '@react-email/components';

   interface MagicLinkEmailProps {
     url: string;
   }

   export function MagicLinkEmail({ url }: MagicLinkEmailProps) {
     return (
       <Html>
         <Head />
         <Preview>Sign in to Mars Vista</Preview>
         <Body style={main}>
           <Container style={container}>
             <Heading style={h1}>Mars Vista</Heading>
             <Text style={text}>
               Click the link below to sign in to your Mars Vista dashboard:
             </Text>
             <Link href={url} style={button}>
               Sign in to Mars Vista
             </Link>
             <Text style={text}>
               This link will expire in 24 hours and can only be used once.
             </Text>
             <Text style={footer}>
               If you didn&apos;t request this email, you can safely ignore it.
             </Text>
           </Container>
         </Body>
       </Html>
     );
   }

   const main = {
     backgroundColor: '#f6f9fc',
     fontFamily: '-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,sans-serif',
   };

   const container = {
     backgroundColor: '#ffffff',
     margin: '0 auto',
     padding: '20px 0 48px',
     marginBottom: '64px',
   };

   const h1 = {
     color: '#333',
     fontSize: '24px',
     fontWeight: 'bold',
     margin: '40px 0',
     padding: '0',
     textAlign: 'center' as const,
   };

   const text = {
     color: '#333',
     fontSize: '16px',
     lineHeight: '26px',
     margin: '16px 0',
     padding: '0 40px',
   };

   const button = {
     backgroundColor: '#000',
     borderRadius: '5px',
     color: '#fff',
     display: 'block',
     fontSize: '16px',
     fontWeight: 'bold',
     textAlign: 'center' as const,
     textDecoration: 'none',
     padding: '12px 20px',
     margin: '24px 40px',
   };

   const footer = {
     color: '#8898aa',
     fontSize: '14px',
     lineHeight: '24px',
     margin: '24px 0',
     padding: '0 40px',
   };
   ```

8. ✅ Create middleware for protected routes:

   Create `middleware.ts`:
   ```typescript
   import { auth } from '@/server/auth-export';
   import { NextResponse } from 'next/server';

   export default auth((req) => {
     const isLoggedIn = !!req.auth;
     const isDashboard = req.nextUrl.pathname.startsWith('/dashboard');

     if (isDashboard && !isLoggedIn) {
       return NextResponse.redirect(new URL('/signin', req.url));
     }

     return NextResponse.next();
   });

   export const config = {
     matcher: ['/((?!api|_next/static|_next/image|favicon.ico).*)'],
   };
   ```

9. ✅ Configure environment variables:

   Create `.env.local`:
   ```bash
   # Database (same as C# API uses)
   DATABASE_URL="postgresql://marsvista:marsvista_dev_password@localhost:5432/marsvista_dev"

   # Resend
   RESEND_API_KEY=re_xxxxx
   FROM_EMAIL=noreply@marsvista.dev

   # Auth.js
   AUTH_SECRET=generate-with-openssl-rand-base64-32
   NEXTAUTH_URL=http://localhost:3000

   # API Backend
   NEXT_PUBLIC_API_URL=http://localhost:5127
   ```

10. ✅ Run Prisma migration:
    ```bash
    npx prisma db push
    npx prisma generate
    ```

11. ✅ Configure Next.js:
    - Set up Tailwind CSS (should be auto-configured)
    - Configure ESLint
    - Add metadata for SEO
    - Set up public assets directory

12. ✅ Create base layout:
    - `app/layout.tsx` with HTML structure
    - Shared metadata (title, description, OG tags)
    - Font loading (system fonts or custom)

13. ✅ Create shared components:
    - `components/Header.tsx` - Navigation (Logo, Docs, Pricing, Sign In, Dashboard)
    - `components/Footer.tsx` - Links, social, copyright
    - `components/Button.tsx` - Reusable CTA buttons
    - `components/Container.tsx` - Max-width wrapper

### Phase 2: Migrate Landing Page (Day 2)

14. ✅ Create `app/page.tsx` (landing page)
15. ✅ Migrate content from `web/landing/src/`:
    - Hero section (tagline, CTA, rover image)
    - Features section (reliability, performance, NASA-compatible)
    - Code example (curl request with syntax highlighting)
    - CTA section (Get started → /signin)
16. ✅ Copy and adapt Tailwind styles
17. ✅ Add OG image and metadata for social sharing
18. ✅ Test locally: `npm run dev`

### Phase 3: Migrate Docs Page (Day 2)

19. ✅ Install Redoc for Next.js:
    ```bash
    npm install redoc
    ```

20. ✅ Create `app/docs/page.tsx`
21. ✅ Integrate Redoc component:
    - Load OpenAPI spec from API server
    - Or bundle OpenAPI spec in public folder
    - Configure Redoc theme to match site
22. ✅ Create `app/docs/layout.tsx` if docs need different sidebar
23. ✅ Test Redoc rendering

### Phase 4: Create New Pages (Day 2-3)

24. ✅ Create `app/pricing/page.tsx`:
    - Pricing table component
    - Free tier: 60/hour, 500/day, $0
    - Pro tier: 5k/hour, 100k/day, $9/month
    - Enterprise tier: Unlimited, custom pricing
    - CTA buttons: "Get Started" → /signin

25. ✅ Create `app/signin/page.tsx`:
    - Email input form
    - "Send Magic Link" button
    - Use Auth.js signIn function:
      ```typescript
      'use client';
      import { signIn } from 'next-auth/react';

      function handleSubmit(email: string) {
        await signIn('resend', { email, callbackUrl: '/dashboard' });
      }
      ```
    - Success → "Check your email for the magic link"
    - Error handling

26. ✅ Create `app/dashboard/page.tsx`:
    - Protected route (middleware redirects if not signed in)
    - Display user email and session info
    - Placeholder for API keys (Story 010 will add backend)
    - Sign out button:
      ```typescript
      'use client';
      import { signOut } from 'next-auth/react';

      <button onClick={() => signOut()}>Sign Out</button>
      ```

27. ✅ Create `app/dashboard/layout.tsx`:
    - Dashboard-specific navigation
    - Sidebar with sections: Overview, API Keys, Usage, Settings
    - User menu with sign out

### Phase 5: Polish and Deploy (Day 3)

28. ✅ Update Header navigation:
    - Home → /
    - Docs → /docs
    - Pricing → /pricing
    - Sign In → /signin (if not authenticated)
    - Dashboard → /dashboard (if authenticated)
    - Status → https://status.marsvista.dev (external)

29. ✅ Update Header to show auth state:
    ```typescript
    import { auth } from '@/server/auth-export';

    const session = await auth();

    {session ? (
      <Link href="/dashboard">Dashboard</Link>
    ) : (
      <Link href="/signin">Sign In</Link>
    )}
    ```

30. ✅ Update Footer:
    - Links: Docs, API Reference, Pricing, Status
    - Social: GitHub repo link
    - Copyright and project info
    - Remove duplicate links

31. ✅ Add loading states and error boundaries:
    - `app/loading.tsx` - Loading UI
    - `app/error.tsx` - Error handling UI
    - `app/dashboard/loading.tsx` - Dashboard loading state

32. ✅ Configure for production:
    - Set up environment variables for API URL
    - Configure `next.config.ts` for Railway deployment
    - Add build script to package.json
    - Test production build: `npm run build`

33. ✅ Deploy to Railway:
    - Create new Railway service for Next.js app
    - Set domain: `marsvista.dev`
    - Configure environment variables:
      ```
      DATABASE_URL=<railway-postgres-url>
      RESEND_API_KEY=re_xxxxx
      FROM_EMAIL=noreply@marsvista.dev
      AUTH_SECRET=<generate-random>
      NEXTAUTH_URL=https://marsvista.dev
      NEXT_PUBLIC_API_URL=https://api.marsvista.dev
      ```
    - Run Prisma migration in Railway
    - Deploy and test

### Phase 6: Cleanup (Day 3)

34. ✅ Remove old Vite apps:
    - Delete `web/landing/` directory
    - Delete `web/docs-site/` directory
    - Update root README.md to reflect new structure

35. ✅ Update documentation:
    - Update `CLAUDE.md` with new web app structure
    - Update deployment docs
    - Document how to run locally
    - Document Auth.js setup

36. ✅ Test all pages in production:
    - Landing page loads
    - Docs render correctly
    - Pricing page displays
    - Sign in flow works (magic link)
    - Dashboard requires authentication
    - Sign out works
    - Navigation works across all pages

## Technical Decisions

### Next.js Configuration

**App Router** (not Pages Router):
- Modern, recommended approach
- Better TypeScript support
- Server components by default (better performance)
- Simpler data fetching

**Deployment**:
- Railway (matches current infrastructure)
- Automatic deployments on git push
- Environment variables for API URL

**Styling**:
- Tailwind CSS (matches existing sites)
- Shared design system across all pages
- Responsive by default

### API Integration

**Client-side API calls** for now:
- Signup form → POST to API server
- Verify page → POST to API server
- Future: Could move to Next.js API routes as proxy

**Environment variables**:
```bash
# .env.local (Development)
DATABASE_URL=postgresql://marsvista:marsvista_dev_password@localhost:5432/marsvista_dev
RESEND_API_KEY=re_xxxxx
FROM_EMAIL=noreply@marsvista.dev
AUTH_SECRET=<openssl rand -base64 32>
NEXTAUTH_URL=http://localhost:3000
NEXT_PUBLIC_API_URL=http://localhost:5127

# Railway (Production)
DATABASE_URL=<railway-postgres-url>
RESEND_API_KEY=re_xxxxx
FROM_EMAIL=noreply@marsvista.dev
AUTH_SECRET=<different-secret-for-prod>
NEXTAUTH_URL=https://marsvista.dev
NEXT_PUBLIC_API_URL=https://api.marsvista.dev
```

### Redoc Integration

**Two options**:

1. **Client-side** (simpler for MVP):
   ```tsx
   import { RedocStandalone } from 'redoc';
   <RedocStandalone specUrl="https://api.marsvista.dev/openapi.json" />
   ```

2. **Server-side** (better SEO):
   - Pre-render Redoc on server
   - Requires custom integration

**Recommendation**: Start with client-side, optimize later if needed.

## Configuration Files

### next.config.ts

```typescript
import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  output: 'standalone', // For Railway deployment
  images: {
    domains: ['api.marsvista.dev'], // If we display rover photos
  },
  async redirects() {
    return [
      // Redirect old paths if needed
    ];
  },
};

export default nextConfig;
```

### package.json

```json
{
  "name": "mars-vista-web",
  "version": "1.0.0",
  "private": true,
  "scripts": {
    "dev": "next dev",
    "build": "next build",
    "start": "next start",
    "lint": "next lint",
    "db:push": "prisma db push",
    "db:generate": "prisma generate",
    "postinstall": "prisma generate"
  },
  "dependencies": {
    "next": "^15.0.0",
    "next-auth": "^5.0.0-beta.29",
    "@auth/prisma-adapter": "^2.11.0",
    "@prisma/client": "^6.17.1",
    "react": "^18.3.1",
    "react-dom": "^18.3.1",
    "redoc": "^2.1.5",
    "resend": "^6.2.2",
    "@react-email/components": "^0.5.7",
    "@react-email/render": "^1.4.0"
  },
  "devDependencies": {
    "@types/node": "^20",
    "@types/react": "^18",
    "@types/react-dom": "^18",
    "autoprefixer": "^10.4.20",
    "eslint": "^8",
    "eslint-config-next": "^15.0.0",
    "postcss": "^8.4.49",
    "prisma": "^6.17.1",
    "tailwindcss": "^3.4.17",
    "typescript": "^5"
  }
}
```

## Deployment

### Railway Configuration

**New service**: `mars-vista-web`
- Domain: `marsvista.dev`
- Build command: `npm run build`
- Start command: `npm start`
- Root directory: `web/app`

**Environment variables**:
- `DATABASE_URL=<railway-postgres-url>` (same database as C# API)
- `RESEND_API_KEY=re_xxxxx`
- `FROM_EMAIL=noreply@marsvista.dev`
- `AUTH_SECRET=<generate-random>`
- `NEXTAUTH_URL=https://marsvista.dev`
- `NEXT_PUBLIC_API_URL=https://api.marsvista.dev`
- `NODE_ENV=production`

**Existing services** (unchanged):
- API server: `api.marsvista.dev`
- Status site: `status.marsvista.dev`
- Database: (private)

## Success Criteria

- ✅ Next.js app runs locally at `http://localhost:3000`
- ✅ Auth.js configured with magic link authentication
- ✅ Prisma schema created and database migrated
- ✅ Landing page migrated with all content and styling
- ✅ Docs page renders Redoc successfully
- ✅ Pricing page displays three tiers
- ✅ Sign in page sends magic links via Resend
- ✅ Dashboard page protected (requires authentication)
- ✅ Sign out functionality works
- ✅ Header shows auth state (Sign In vs Dashboard)
- ✅ Footer links are correct
- ✅ Deployed to Railway at `https://marsvista.dev`
- ✅ Old Vite apps deleted
- ✅ Responsive design on mobile/tablet/desktop

## Testing Checklist

**Local development**:
- [ ] `npm run dev` starts successfully
- [ ] Prisma client generates correctly
- [ ] All routes render without errors (/, /docs, /pricing, /signin, /dashboard)
- [ ] Navigation between pages works
- [ ] Tailwind styles applied correctly
- [ ] Redoc loads in /docs
- [ ] No console errors

**Auth.js functionality**:
- [ ] Sign in page accepts email
- [ ] Magic link email sent via Resend
- [ ] Magic link email renders correctly (test in email client)
- [ ] Clicking magic link authenticates user
- [ ] User redirected to /dashboard after sign in
- [ ] Dashboard shows user session info
- [ ] Sign out clears session and redirects to /
- [ ] Accessing /dashboard without auth redirects to /signin
- [ ] Session persists across page reloads

**Production build**:
- [ ] `npm run build` succeeds
- [ ] `npm start` serves production app
- [ ] All pages accessible
- [ ] Images load correctly
- [ ] Lighthouse score > 90 for performance

**Deployment**:
- [ ] Railway build succeeds
- [ ] Prisma migrations run automatically (postinstall)
- [ ] Domain `marsvista.dev` resolves correctly
- [ ] HTTPS works
- [ ] All pages load in production
- [ ] Magic link authentication works in production
- [ ] Session cookies secure (httpOnly, sameSite)
- [ ] Database connection works (Auth.js + C# API share same PostgreSQL)

## Future Enhancements (Not in this story)

- [ ] Dashboard API key management - Story 010 (backend) + Story 011 (UI)
- [ ] Usage statistics and analytics in dashboard
- [ ] Billing/payment integration for Pro tier
- [ ] Blog section (`/blog`) for updates and tutorials
- [ ] Interactive API explorer (try API in browser)
- [ ] Rover photo gallery showcase
- [ ] Search functionality for docs
- [ ] Dark mode toggle
- [ ] Analytics (Plausible or similar)
- [ ] OAuth providers (Google, GitHub) in addition to magic links

## Dependencies

- Node.js 20+ installed
- PostgreSQL database (shared with C# API)
- Resend account with API key
- Railway account and CLI configured
- Domain `marsvista.dev` configured in Railway
- Email domain verified in Resend (`marsvista.dev`)

## Story Completion Checklist

When marking this story as complete:
- [ ] All pages migrated and working
- [ ] Deployed to Railway production
- [ ] Old Vite apps deleted
- [ ] Documentation updated (CLAUDE.md, README.md)
- [ ] Commit message follows commit-guide skill
- [ ] Changes pushed to GitHub
- [ ] Ready for Story 010 (API key authentication)

## Notes

- This story includes Auth.js setup for **dashboard sessions** (sign in/out)
- Story 010 will add **API key authentication** for API requests (different concern)
- Dashboard will show API keys but can't create/delete them yet (Story 010 backend + Story 011 UI)
- Auth.js creates `User`, `Session`, `VerificationToken` tables (separate from API `users` table)
- Both Auth.js and C# API use the same PostgreSQL database
- Keep status site completely separate - different domain, different purpose
- Focus on getting Next.js + Auth.js foundation solid - can iterate on design later

## Important: Two Auth Systems

**Auth.js (this story)**:
- Purpose: Dashboard sessions (web app sign in/out)
- Tables: `User`, `Session`, `VerificationToken`
- Managed by: Prisma (Next.js)
- Used for: `/dashboard` access control

**API Key Auth (Story 010)**:
- Purpose: API request authentication
- Tables: `users` (with `api_key_hash`, `tier`)
- Managed by: EF Core (C# API)
- Used for: `/api/v1/*` requests

**Link**: Both systems share same PostgreSQL database, linked by email
- `User.email` (Auth.js) ↔ `users.email` (API)
- Dashboard queries both to show user's API keys

## Estimated Effort
3 days (can be done faster if focused)
