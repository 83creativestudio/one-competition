import { cookies } from "next/headers";

export const accessCookie = "one_access";
export const refreshCookie = "one_refresh";
export const profileCookie = "one_profile";
export const tenantCookie = "one_tenant_slug";

export function apiBaseUrl() {
  return process.env.API_BASE_URL ?? process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5050";
}

export const authCookieOptions = {
  httpOnly: true,
  secure: process.env.NODE_ENV === "production",
  sameSite: "strict" as const,
  path: "/"
};

export async function getSessionProfile() {
  const value = (await cookies()).get(profileCookie)?.value;
  if (!value) return null;
  try { return JSON.parse(Buffer.from(value, "base64url").toString("utf8")) as { userId: string; email: string; roles: string[]; tenants: unknown[] }; }
  catch { return null; }
}
