import { NextResponse } from "next/server";
import { accessCookie, profileCookie, refreshCookie, tenantCookie } from "@/lib/server-auth";

export async function POST() {
  const response = NextResponse.json({ ok: true });
  for (const name of [accessCookie, refreshCookie, profileCookie, tenantCookie]) response.cookies.delete(name);
  return response;
}
