import { NextRequest, NextResponse } from "next/server";

export async function GET(request: NextRequest, { params }: { params: Promise<{ tenantSlug: string; assetId: string }> }) {
  const { tenantSlug, assetId } = await params;
  if (!/^[a-z0-9-]{1,120}$/.test(tenantSlug) || !/^[a-f0-9-]{36}$/i.test(assetId)) return new NextResponse(null, { status: 404 });
  const upstream = await fetch(`${process.env.API_BASE_URL ?? "http://127.0.0.1:5050"}/c/${tenantSlug}/api/public/media/${assetId}`, {
    cache: "no-store", headers: { Host: request.headers.get("host") ?? request.nextUrl.host,
      ...(request.headers.get("range") ? { Range: request.headers.get("range")! } : {}) }
  });
  const headers = new Headers({ "X-Content-Type-Options": "nosniff" });
  for (const key of ["content-type", "content-length", "content-range", "accept-ranges", "etag", "last-modified", "cache-control"]) {
    const value = upstream.headers.get(key); if (value) headers.set(key, value);
  }
  return new NextResponse(upstream.body, { status: upstream.status, headers });
}
