import NextAuth from 'next-auth';
import { authConfig } from './auth';

export const { handlers, auth, signIn, signOut } = NextAuth(authConfig);
