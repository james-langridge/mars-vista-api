import { auth } from '@/server/auth-export';
import SiteHeader from './SiteHeader';

export default async function SiteHeaderWrapper() {
  const session = await auth();
  return <SiteHeader isAuthenticated={!!session} />;
}
