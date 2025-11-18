import { handlers } from '@/server/auth-export';

// Force Node.js runtime for Prisma compatibility
export const runtime = 'nodejs';

export const { GET, POST } = handlers;
