# Decision 020: Authentication Strategy for Dashboard (Auth.js vs Custom)

## Context

We need two types of authentication for Mars Vista:

1. **API Key Authentication** (for API requests)
   - Stateless
   - `X-API-Key` header
   - No sessions needed
   - Already decided: Custom implementation (Story 010)

2. **Session-based Authentication** (for dashboard)
   - Stateful (users sign in to web app)
   - Sign in/sign out
   - Protected routes (/dashboard)
   - Magic link authentication
   - Need to manage API keys (view, create, delete)

## The Question

Should we use **Auth.js (NextAuth v5)** for dashboard authentication, or continue with **custom implementation**?

## Reference Implementation

User has working Auth.js implementation in quote-vault:
- NextAuth v5 (beta.29)
- Resend provider for magic links
- Prisma adapter
- Custom email templates with `@react-email/components`
- Session callbacks for user ID
- Protected API routes with `auth()` helper

## Analysis

### Option 1: Auth.js (NextAuth v5)

**Implementation** (based on quote-vault):

```typescript
// server/auth.ts
import { PrismaAdapter } from '@auth/prisma-adapter';
import Resend from 'next-auth/providers/resend';

export const authConfig = {
  providers: [
    Resend({
      from: 'noreply@marsvista.dev',
      sendVerificationRequest: async ({ identifier: email, url }) => {
        const html = await render(MagicLinkEmail({ url }));
        await sendEmail({ to: email, subject: 'Sign in to Mars Vista', html });
      },
    }),
  ],
  adapter: PrismaAdapter(db), // Uses Prisma with PostgreSQL
  callbacks: {
    session: ({ session, user }) => ({
      ...session,
      user: { ...session.user, id: user.id },
    }),
  },
};
```

**Pros**:
- ✅ **Battle-tested**: Used by thousands of Next.js apps
- ✅ **Magic link built-in**: Resend provider already supports it
- ✅ **Session management**: Handles cookies, CSRF, security automatically
- ✅ **Database adapter**: Prisma adapter for PostgreSQL (stores users, sessions, verification tokens)
- ✅ **Protected routes**: Middleware for protecting /dashboard routes
- ✅ **You're familiar**: Already implemented in quote-vault
- ✅ **Email templates**: Can use `@react-email/components` for nice emails
- ✅ **Sign in/out helpers**: `signIn()`, `signOut()`, `auth()` utilities
- ✅ **Future extensibility**: Can add OAuth (Google, GitHub) later

**Cons**:
- ⚠️ **Dual user tables**: Auth.js creates its own `User` table, we already have `users` table for API keys
- ⚠️ **Complexity**: Adds Auth.js concepts (providers, adapters, callbacks)
- ⚠️ **Database coupling**: Requires Prisma adapter (we're using EF Core for C# API)
- ⚠️ **Another dependency**: NextAuth + adapter + email libs
- ⚠️ **Version uncertainty**: Still in beta (v5.0.0-beta.29)

### Option 2: Custom Implementation

**Implementation**:

```typescript
// Custom magic link flow
// 1. User enters email → Generate token → Send via Resend
// 2. User clicks link → Validate token → Create session cookie
// 3. Protected routes check session cookie
```

**Pros**:
- ✅ **Single user table**: Use same `users` table for both API keys and dashboard
- ✅ **Full control**: Understand every piece of auth logic
- ✅ **No extra tables**: Auth.js creates 4+ tables (users, sessions, verification_tokens, accounts)
- ✅ **Simpler**: Less abstraction, more explicit
- ✅ **Matches API backend**: Same patterns as C# API auth

**Cons**:
- ❌ **Security risk**: Easy to make mistakes (CSRF, session fixation, timing attacks)
- ❌ **More code**: Need to write session management, cookies, token validation
- ❌ **Testing burden**: Need comprehensive security testing
- ❌ **Missing features**: No OAuth, no account linking, no adapters
- ❌ **Reinventing wheel**: Auth.js already solves these problems

## Architecture Considerations

### With Auth.js (Two User Tables)

**Auth.js tables** (created by Prisma adapter):
```sql
-- Managed by Auth.js
CREATE TABLE "User" (
  id TEXT PRIMARY KEY,
  email TEXT UNIQUE,
  emailVerified TIMESTAMP,
  image TEXT,
  name TEXT
);

CREATE TABLE "Session" (
  id TEXT PRIMARY KEY,
  sessionToken TEXT UNIQUE,
  userId TEXT REFERENCES "User"(id),
  expires TIMESTAMP
);

CREATE TABLE "VerificationToken" (
  identifier TEXT,
  token TEXT,
  expires TIMESTAMP,
  PRIMARY KEY (identifier, token)
);
```

**Our API key table** (in PostgreSQL via C# API):
```sql
-- Managed by EF Core (C# API)
CREATE TABLE users (
  id UUID PRIMARY KEY,
  email VARCHAR(255) UNIQUE,
  api_key_hash VARCHAR(64),
  tier VARCHAR(20),
  created_at TIMESTAMP
);
```

**Problem**: Two separate user systems!

**Solution options**:

1. **Link tables by email**:
   - Auth.js `User.email` ↔ API `users.email`
   - Dashboard queries both tables
   - Keep them in sync

2. **Auth.js as source of truth**:
   - Store API keys in Auth.js User table (custom fields)
   - C# API reads from Auth.js tables
   - **Problem**: EF Core + Prisma schema conflict

3. **Separate concerns**:
   - Auth.js ONLY for dashboard sessions
   - API `users` table ONLY for API keys
   - Link by email
   - Accept duplication

### With Custom Auth (Single User Table)

**One user table**:
```sql
CREATE TABLE users (
  id UUID PRIMARY KEY,
  email VARCHAR(255) UNIQUE,
  api_key_hash VARCHAR(64),
  tier VARCHAR(20),
  created_at TIMESTAMP,
  -- Dashboard session fields
  session_token VARCHAR(255),
  session_expires TIMESTAMP,
  magic_link_token_hash VARCHAR(64),
  magic_link_expires TIMESTAMP
);
```

**Advantage**: Single source of truth for users.

**Disadvantage**: Need to implement session management manually.

## Hybrid Approach (Recommended)

Use **Auth.js for dashboard** + **Keep custom API key auth**, but integrate them:

### Database Strategy

**Option A: Prisma alongside EF Core**

Next.js uses Prisma (for Auth.js) → same PostgreSQL database ← C# API uses EF Core

```
PostgreSQL Database
├── Auth.js tables (managed by Prisma in Next.js)
│   ├── User (id, email, emailVerified)
│   ├── Session
│   └── VerificationToken
├── API tables (managed by EF Core in C# API)
│   ├── users (id, email, api_key_hash, tier)
│   ├── photos
│   ├── rovers
│   └── cameras
└── Link: User.email = users.email
```

**Schema sync**:
```typescript
// Next.js: prisma/schema.prisma
model User {
  id            String    @id @default(cuid())
  email         String?   @unique
  emailVerified DateTime?
  // ... Auth.js fields
}

// C# API: Models/User.cs (different table name)
public class User {
  public Guid Id { get; set; }
  public string Email { get; set; }
  public string ApiKeyHash { get; set; }
  // ... API key fields
}

// Table names: "User" (Auth.js) vs "users" (API)
```

**Linking**:
```typescript
// Dashboard: Get user's API keys
const session = await auth();
const apiUser = await db.query('SELECT * FROM users WHERE email = $1', [session.user.email]);
const apiKeys = apiUser.api_key_hash;
```

**Option B: Shared Prisma Schema (Not Recommended)**

Try to make C# API use Prisma schema → **Very complex**, don't do this.

## Recommendation: **Auth.js for Dashboard**

### Rationale

1. **Security**: Don't reinvent auth. Auth.js is battle-tested.
2. **Velocity**: You already know Auth.js from quote-vault. Fast to implement.
3. **Features**: Magic links, OAuth (future), session management all included.
4. **Maintainability**: Well-documented, active community support.
5. **Acceptable complexity**: Two user tables is manageable via email linking.

### Implementation Plan

**Auth.js for**:
- ✅ Sign in to dashboard (magic link)
- ✅ Session management
- ✅ Protected routes (/dashboard/*)
- ✅ Sign out

**Custom API key auth for**:
- ✅ API requests (`X-API-Key` header)
- ✅ Rate limiting
- ✅ Tier management

**Integration**:
- Link by email: `User.email` (Auth.js) ↔ `users.email` (API)
- Dashboard shows API keys by querying both tables
- Create new API keys → insert into `users` table (C# API endpoint)

### Database Schema

**Auth.js tables** (Next.js Prisma):
```prisma
model User {
  id            String    @id @default(cuid())
  email         String?   @unique
  emailVerified DateTime?
  name          String?
  image         String?
  sessions      Session[]
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

**API key table** (C# EF Core):
```csharp
public class User {
  public Guid Id { get; set; }
  public string Email { get; set; }
  public string ApiKeyHash { get; set; }
  public string Tier { get; set; } = "free";
  public DateTime CreatedAt { get; set; }
  public DateTime? LastRequestAt { get; set; }
}
```

**Note**: Different table names prevent conflicts (`User` vs `users`).

## Alternative: Unified User Table (Not Recommended)

Could we use a single `users` table for both?

**Option**: Make Auth.js use our existing `users` table:

```prisma
model users {
  id               String    @id @default(dbgenerated("gen_random_uuid()")) @db.Uuid
  email            String    @unique @db.VarChar(255)
  api_key_hash     String?   @db.VarChar(64)
  tier             String    @default("free") @db.VarChar(20)
  created_at       DateTime  @default(now())
  // Auth.js required fields
  emailVerified    DateTime? @map("email_verified_at")
  name             String?
  image            String?
  sessions         Session[]

  @@map("users")
}
```

**Problems**:
- EF Core (C# API) and Prisma (Next.js) both managing same table
- Schema drift risk (migrations from two sources)
- Deployment complexity (which migrates first?)

**Verdict**: Don't do this. Separate tables is cleaner.

## Implementation Summary

### Stack

**Next.js (Dashboard)**:
- Auth.js v5 (NextAuth)
- Resend provider (magic links)
- Prisma adapter
- `@react-email/components` for email templates
- Session-based auth for /dashboard routes

**C# API (API Keys)**:
- Custom API key authentication
- EF Core for `users` table
- Rate limiting middleware

**Database**:
- PostgreSQL (shared)
- Auth.js tables: `User`, `Session`, `VerificationToken`
- API tables: `users`, `photos`, `rovers`, `cameras`
- Link: `User.email = users.email`

### Dashboard Flow

1. User visits `/dashboard` (not signed in) → Redirect to `/signin`
2. User enters email → Auth.js sends magic link via Resend
3. User clicks link → Auth.js validates token → Creates session
4. User redirected to `/dashboard` → Shows API keys
5. Dashboard queries: `SELECT * FROM users WHERE email = $1`
6. Display API keys, tier, usage stats
7. Actions: Create new key, delete key (calls C# API endpoints)

## Related Stories

- Story 009: Unified Next.js Frontend (landing, docs, pricing)
- Story 010: API Key Authentication (C# API)
- Story 011: Dashboard with Auth.js (this decision)

## Decision

**Use Auth.js (NextAuth v5) for dashboard authentication.**

- Proven, secure, feature-rich
- Quick to implement (copy from quote-vault)
- Accept two user tables, link by email
- Future-proof for OAuth

**Update Story 009** to include Auth.js setup.
**Update Story 010** to clarify API key auth is separate from dashboard auth.
