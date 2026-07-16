import { NextRequest, NextResponse } from "next/server";
import { accessCookie } from "@/lib/server-auth";

export function proxy(request: NextRequest) {
  if ((request.nextUrl.pathname.startsWith("/dashboard") || request.nextUrl.pathname.startsWith("/platform")) && !request.cookies.has(accessCookie)) {
    return NextResponse.redirect(new URL(`/login?returnTo=${encodeURIComponent(request.nextUrl.pathname)}`, request.url));
  }
  return NextResponse.next();
}

export const config = { matcher: ["/dashboard/:path*", "/platform/:path*"] };
