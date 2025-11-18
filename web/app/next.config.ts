import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Include Prisma client files in build output for deployment
  outputFileTracingIncludes: {
    '/*': ['./node_modules/.prisma/client/**/*'],
  },
};

export default nextConfig;
