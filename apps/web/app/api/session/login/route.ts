import { NextRequest, NextResponse } from "next/server";
import { accessCookie, apiBaseUrl, authCookieOptions, profileCookie, refreshCookie, tenantCookie } from "@/lib/server-auth";

type AuthResponse = { accessToken: string; refreshToken: string; expiresAt: string; userId: string; email: string; roles: string[]; tenants: { slug: string }[] };

export async function POST(request: NextRequest) {
  const body = await request.json();
  const response = await fetch(`${apiBaseUrl()}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Host: request.nextUrl.host, "X-Forwarded-Proto": request.nextUrl.protocol.replace(":", "") },
    body: JSON.stringify(body),
    cache: "no-store"
  });
  const payload = await response.json().catch(() => ({})) as AuthResponse & { detail?: string };
  if (!response.ok) return NextResponse.json(payload, { status: response.status });
  const result = NextResponse.json({ userId: payload.userId, email: payload.email, roles: payload.roles, tenants: payload.tenants });
  result.cookies.set(accessCookie, payload.accessToken, { ...authCookieOptions, expires: new Date(payload.expiresAt) });
  result.cookies.set(refreshCookie, payload.refreshToken, { ...authCookieOptions, expires: new Date(Date.now() + 14 * 86400000) });
  result.cookies.set(profileCookie, Buffer.from(JSON.stringify({ userId: payload.userId, email: payload.email, roles: payload.roles, tenants: payload.tenants })).toString("base64url"), { ...authCookieOptions, expires: new Date(Date.now() + 14 * 86400000) });
  if (payload.tenants[0]) result.cookies.set(tenantCookie, payload.tenants[0].slug, { ...authCookieOptions, expires: new Date(Date.now() + 14 * 86400000) });
  return result;
}
