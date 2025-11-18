import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Include Prisma client files in build output for deployment
  outputFileTracingIncludes: {
    '/*': ['./node_modules/.prisma/client/**/*'],
  },
  // Ensure Prisma runs in Node.js runtime, not edge
  serverExternalPackages: ['@prisma/client', 'prisma'],
};

export default nextConfig;
