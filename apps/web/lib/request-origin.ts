export function isAllowedRequestOrigin(headers: Headers, fallbackHost: string, requireOrigin = false): boolean {
  if (headers.get("sec-fetch-site") === "cross-site") return false;
  const origin = headers.get("origin");
  if (!origin) return !requireOrigin;
  const host = headers.get("host") ?? fallbackHost;
  if (!/^[a-z0-9.:[\]-]+$/i.test(host)) return false;
  try {
    const source = new URL(origin);
    if (!["https:", "http:"].includes(source.protocol) || source.username || source.password
      || source.pathname !== "/" || source.search || source.hash) return false;
    // The reverse proxy sets Host to the public authority; nextUrl can contain the container hostname.
    return source.host === new URL(`${source.protocol}//${host}`).host;
  } catch {
    return false;
  }
}
