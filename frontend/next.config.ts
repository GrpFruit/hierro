import path from "node:path";
import type { NextConfig } from "next";

// Where the .NET API listens. Matches the "http" profile in backend/Hierro.Api/Properties/launchSettings.json.
const apiUrl = process.env.HIERRO_API_URL ?? "http://localhost:5115";

const nextConfig: NextConfig = {
  cacheComponents: true,
  partialPrefetching: true,
  // Pin the workspace root: a stray lockfile in a parent folder would otherwise be picked up.
  turbopack: { root: path.resolve(__dirname) },
  async rewrites() {
    return [{ source: "/api/:path*", destination: `${apiUrl}/api/:path*` }];
  },
};

export default nextConfig;
