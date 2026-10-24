import { pathToFileURL } from "node:url";

// Configuration-only gate. Never print values, connection strings or credentials.
export function validateOmegaConfiguration(env) {
  const failures = [];
  const requireValue = (key) => {
    const value = env[key]?.trim();
    if (!value || /use-secret-manager|replace[_-]?me|example\.(com|internal)|development-only/i.test(value))
      failures.push(`${key}: missing or placeholder configuration`);
    return value;
  };
  const expect = (key, value) => {
    if (env[key]?.toLowerCase() !== value.toLowerCase()) failures.push(`${key}: must be ${value}`);
  };
  expect("ASPNETCORE_ENVIRONMENT", "Production");
  expect("DATABASE_PROVIDER", "postgres");
  expect("DISTRIBUTED_LOCK_PROVIDER", "redis");
  expect("EMAIL_PROVIDER", "smtp");
  expect("CAPTCHA_PROVIDER", "turnstile");
  expect("OBJECT_STORAGE_PROVIDER", "s3");
  expect("VIRUS_SCANNER_PROVIDER", "http");
  expect("DOMAIN_VERIFICATION_PROVIDER", "dns");
  if (env.SMTP_USE_TLS?.toLowerCase() === "false") failures.push("SMTP_USE_TLS: TLS must not be disabled");
  for (const key of ["DATABASE_CONNECTION_STRING", "REDIS_CONNECTION_STRING", "EMAIL_FROM_ADDRESS",
    "SMTP_HOST", "SMTP_USERNAME", "SMTP_PASSWORD", "TURNSTILE_SITE_KEY", "TURNSTILE_SECRET_KEY",
    "OBJECT_STORAGE_ENDPOINT", "OBJECT_STORAGE_BUCKET", "VIRUS_SCANNER_ENDPOINT",
    "PLATFORM_BASE_DOMAIN", "PLATFORM_AUTH_DOMAIN", "GOOGLE_CLIENT_ID", "GOOGLE_CLIENT_SECRET",
    "APPLE_CLIENT_ID", "APPLE_TEAM_ID", "APPLE_KEY_ID", "APPLE_PRIVATE_KEY",
    "FACEBOOK_CLIENT_ID", "FACEBOOK_CLIENT_SECRET"]) requireValue(key);
  const signingKey = requireValue("JWT_SIGNING_KEY");
  if (signingKey && Buffer.byteLength(signingKey) < 32) failures.push("JWT_SIGNING_KEY: at least 32 bytes required");
  const encryptionKey = requireValue("ENCRYPTION_KEY");
  if (encryptionKey && (!/^[A-Za-z0-9+/]{43}=$/.test(encryptionKey)
    || Buffer.from(encryptionKey, "base64").length !== 32)) failures.push("ENCRYPTION_KEY: a base64-encoded 32-byte key is required");
  if (!env.FORWARDED_HEADERS_KNOWN_NETWORKS?.trim() && !env.FORWARDED_HEADERS_KNOWN_PROXIES?.trim())
    failures.push("FORWARDED_HEADERS: configure a trusted proxy network or address");
  for (const key of ["OBJECT_STORAGE_ENDPOINT", "VIRUS_SCANNER_ENDPOINT"]) {
    try {
      const url = new URL(env[key]);
      if (url.protocol !== "https:" || url.username || url.password) throw new Error();
    } catch { failures.push(`${key}: HTTPS URL without embedded credentials required`); }
  }
  return failures;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const failures = validateOmegaConfiguration(process.env);
  for (const failure of failures) console.error(`FAIL ${failure}`);
  if (failures.length) process.exitCode = 1;
  else console.log("Configuration gate passed. Provider delivery, MFA enrollment, capacity, recovery and approvals still require evidence.");
}
