import { NextRequest, NextResponse } from "next/server";
import { accessCookie, apiBaseUrl, authCookieOptions, participantSessionCookie, refreshCookie, tenantCookie } from "@/lib/server-auth";

async function forward(request: NextRequest, segments: string[]) {
  if (!["GET", "HEAD", "OPTIONS"].includes(request.method)) {
    const origin = request.headers.get("origin");
    if (origin && new URL(origin).host !== request.nextUrl.host)
      return NextResponse.json({ title: "Forbidden", detail: "Cross-site request rejected." }, { status: 403 });
  }
  const jar = request.cookies;
  let access = jar.get(accessCookie)?.value;
  const refresh = jar.get(refreshCookie)?.value;
  const target = new URL(`${apiBaseUrl()}/${segments.join("/")}`);
  request.nextUrl.searchParams.forEach((value, key) => target.searchParams.append(key, value));
  const body = request.method === "GET" || request.method === "HEAD" ? undefined : await request.arrayBuffer();
  const send = (token?: string) => fetch(target, {
    method: request.method,
    headers: {
      ...(request.headers.get("content-type") ? { "Content-Type": request.headers.get("content-type")! } : {}),
      Host: request.nextUrl.host,
      "X-Forwarded-Proto": request.nextUrl.protocol.replace(":", ""),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(jar.get(participantSessionCookie)?.value ? { "X-One-Participant-Session": jar.get(participantSessionCookie)!.value } : {}),
      ...(jar.get(tenantCookie)?.value ? { "X-One-Tenant": jar.get(tenantCookie)!.value } : {})
    },
    body,
    cache: "no-store",
    redirect: "manual"
  });
  let upstream = await send(access);
  let refreshed: { accessToken: string; refreshToken: string; expiresAt: string } | null = null;
  if (upstream.status === 401 && refresh) {
    const refreshResponse = await fetch(`${apiBaseUrl()}/api/auth/refresh`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ refreshToken: refresh }), cache: "no-store" });
    if (refreshResponse.ok) {
      refreshed = await refreshResponse.json();
      access = refreshed!.accessToken;
      upstream = await send(access);
    }
  }
  const response = new NextResponse(upstream.body, { status: upstream.status, headers: { "Content-Type": upstream.headers.get("content-type") ?? "application/json" } });
  const location = upstream.headers.get("location");
  if (location) response.headers.set("Location", location.startsWith("/api/") ? `/api/backend${location}` : location);
  if (refreshed) {
    response.cookies.set(accessCookie, refreshed.accessToken, { ...authCookieOptions, expires: new Date(refreshed.expiresAt) });
    response.cookies.set(refreshCookie, refreshed.refreshToken, { ...authCookieOptions, expires: new Date(Date.now() + 14 * 86400000) });
  }
  return response;
}

type RouteContext = { params: Promise<{ path: string[] }> };
export async function GET(request: NextRequest, context: RouteContext) { return forward(request, (await context.params).path); }
export async function POST(request: NextRequest, context: RouteContext) { return forward(request, (await context.params).path); }
export async function PUT(request: NextRequest, context: RouteContext) { return forward(request, (await context.params).path); }
export async function PATCH(request: NextRequest, context: RouteContext) { return forward(request, (await context.params).path); }
export async function DELETE(request: NextRequest, context: RouteContext) { return forward(request, (await context.params).path); }
