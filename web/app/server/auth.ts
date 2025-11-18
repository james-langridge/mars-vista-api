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

export const authConfig = {
  trustHost: true,
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
