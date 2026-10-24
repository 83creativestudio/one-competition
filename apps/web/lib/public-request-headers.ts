export function publicRequestHeaders(headers: Pick<Headers, "get">, fallbackHost = "localhost") {
  const host = headers.get("host") ?? fallbackHost;
  return {
    "X-Forwarded-Host": host,
    "X-Forwarded-Proto": headers.get("x-forwarded-proto") === "https" ? "https" : "http"
  };
}
