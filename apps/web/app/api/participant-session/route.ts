import { NextRequest, NextResponse } from "next/server";
import { apiBaseUrl, authCookieOptions, participantSessionCookie } from "@/lib/server-auth";

export async function POST(request: NextRequest) {
  const origin = request.headers.get("origin");
  if (origin && new URL(origin).host !== request.nextUrl.host)
    return NextResponse.json({ title: "Forbidden", detail: "Cross-site request rejected." }, { status: 403 });
  const body = await request.json() as { code?: string; tenantSlug?: string };
  if (!body.code) return NextResponse.json({ title: "Invalid request", detail: "Completion code is required." }, { status: 400 });
  const path = body.tenantSlug ? `/c/${encodeURIComponent(body.tenantSlug)}/api/public/social-auth/complete` : "/api/public/social-auth/complete";
  const upstream = await fetch(`${apiBaseUrl()}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Host: request.nextUrl.host, "X-Forwarded-Proto": request.nextUrl.protocol.replace(":", "") },
    body: JSON.stringify({ code: body.code }),
    cache: "no-store"
  });
  const payload = await upstream.json().catch(() => null) as ({ participantSessionToken?: string } & Record<string, unknown>) | null;
  if (!upstream.ok || !payload?.participantSessionToken)
    return NextResponse.json(payload ?? { title: "Social login failed" }, { status: upstream.status || 502 });
  const { participantSessionToken, ...profile } = payload;
  const response = NextResponse.json(profile);
  response.cookies.set(participantSessionCookie, participantSessionToken, { ...authCookieOptions, maxAge: 30 * 60 });
  return response;
}
