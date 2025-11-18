import type { Metadata } from 'next';
import RedocWrapper from '@/components/RedocWrapper';

export const metadata: Metadata = {
  title: 'API Documentation - Mars Vista API',
  description: 'Complete API documentation for Mars Vista rover imagery API',
};

export default function Docs() {
  return (
    <div className="bg-white">
      <RedocWrapper />
    </div>
  );
}
