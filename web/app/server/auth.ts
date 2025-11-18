import { PrismaAdapter } from '@auth/prisma-adapter';
import { type DefaultSession, type NextAuthConfig } from 'next-auth';
import Resend from 'next-auth/providers/resend';
import { db } from './db';
import { render } from '@react-email/render';
import { MagicLinkEmail } from '@/components/emails/MagicLinkEmail';
import { Resend as ResendClient } from 'resend';

function getResendClient() {
  return new ResendClient(process.env.RESEND_API_KEY);
}

declare module 'next-auth' {
  interface Session extends DefaultSession {
    user: {
      id: string;
    } & DefaultSession['user'];
  }
}

declare module 'next-auth/jwt' {
  interface JWT {
    id: string;
  }
}

export const authConfig = {
  trustHost: true,
  // Use JWT strategy for edge-compatible sessions (middleware can verify without DB)
  session: {
    strategy: 'jwt',
  },
  providers: [
    Resend({
      from: process.env.FROM_EMAIL || 'noreply@notifications.marsvista.dev',
      sendVerificationRequest: async ({ identifier: email, url }) => {
        try {
          const html = await render(MagicLinkEmail({ url }));
          const resend = getResendClient();

          await resend.emails.send({
            from: process.env.FROM_EMAIL || 'noreply@notifications.marsvista.dev',
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
    // With JWT strategy, we need to populate the token with user id
    jwt: async ({ token, user }) => {
      if (user) {
        token.id = user.id;
      }
      return token;
    },
    // Then pass it to the session
    session: ({ session, token }) => ({
      ...session,
      user: {
        ...session.user,
        id: token.id as string,
      },
    }),
  },
  pages: {
    signIn: '/signin',
    error: '/signin',
  },
} satisfies NextAuthConfig;
