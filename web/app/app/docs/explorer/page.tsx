import type { Metadata } from 'next';
import ExplorerClient from './ExplorerClient';

export const metadata: Metadata = {
  title: 'API Explorer - Mars Vista API',
  description: 'Interactively explore Mars rover photos using the v2 API',
};

export default function ExplorerPage() {
  return <ExplorerClient />;
}
