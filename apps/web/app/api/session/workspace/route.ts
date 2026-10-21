import { NextRequest, NextResponse } from "next/server";
import { isAllowedRequestOrigin } from "@/lib/request-origin";
import { accessCookie, apiBaseUrl, authCookieOptions, tenantCookie } from "@/lib/server-auth";

export async function POST(request: NextRequest) {
  if (!isAllowedRequestOrigin(request.headers, request.nextUrl.host, true))
    return NextResponse.json({ detail: "Cross-site request rejected." }, { status: 403 });
  const access = request.cookies.get(accessCookie)?.value;
  if (!access) return NextResponse.json({ detail: "Please sign in again." }, { status: 401 });
  const body = await request.json() as { slug?: unknown };
  if (typeof body.slug !== "string" || !/^[a-z0-9-]{1,120}$/.test(body.slug))
    return NextResponse.json({ detail: "Workspace is invalid." }, { status: 400 });
  const upstream = await fetch(`${apiBaseUrl()}/api/tenants/current`, { cache: "no-store", headers: {
    Authorization: `Bearer ${access}`, "X-One-Tenant": body.slug,
    "X-Forwarded-Host": request.headers.get("x-forwarded-host") ?? request.headers.get("host") ?? request.nextUrl.host,
    "X-Forwarded-Proto": request.headers.get("x-forwarded-proto") ?? request.nextUrl.protocol.replace(":", "")
  } });
  if (!upstream.ok) return NextResponse.json({ detail: "Workspace access is unavailable. Please sign in again or contact your administrator." }, { status: upstream.status });
  const tenant = await upstream.json() as { slug: string };
  if (tenant.slug !== body.slug) return NextResponse.json({ detail: "This hostname belongs to a different workspace." }, { status: 403 });
  const response = NextResponse.json({ slug: tenant.slug });
  response.cookies.set(tenantCookie, tenant.slug, { ...authCookieOptions, maxAge: 14 * 86400 });
  return response;
}
