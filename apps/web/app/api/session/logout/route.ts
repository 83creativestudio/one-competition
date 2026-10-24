import { NextRequest, NextResponse } from "next/server";
import { isAllowedRequestOrigin } from "@/lib/request-origin";
import { accessCookie, profileCookie, refreshCookie, tenantCookie } from "@/lib/server-auth";

export async function POST(request: NextRequest) {
  if (!isAllowedRequestOrigin(request.headers, request.nextUrl.host, true))
    return NextResponse.json({ detail: "Cross-site request rejected." }, { status: 403 });
  const response = NextResponse.json({ ok: true });
  for (const name of [accessCookie, refreshCookie, profileCookie, tenantCookie]) response.cookies.delete(name);
  return response;
}
