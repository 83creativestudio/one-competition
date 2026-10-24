import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { validateOmegaConfiguration } from "./omega-preflight.mjs";

test("S3 examples restrict browser origins and runtime object permissions", async () => {
  const directory = new URL("../deployment/omega/", import.meta.url);
  const cors = JSON.parse(await readFile(new URL("s3-cors.example.json", directory), "utf8"));
  assert.deepEqual(cors[0].AllowedOrigins, ["https://competition.onedigital.com.cy"]);
  assert.ok(!cors[0].AllowedMethods.includes("DELETE"));
  const policy = JSON.parse(await readFile(new URL("s3-runtime-policy.example.json", directory), "utf8"));
  assert.deepEqual(policy.Statement[0].Action, ["s3:PutObject", "s3:GetObject", "s3:DeleteObject"]);
  assert.deepEqual(policy.Statement[0].Resource, [
    "arn:aws:s3:::REPLACE_WITH_BUCKET_NAME/tenant/*",
    "arn:aws:s3:::REPLACE_WITH_BUCKET_NAME/system/health/*"
  ]);
});

test("fails closed for empty configuration without disclosing values", () => {
  assert.ok(validateOmegaConfiguration({}).length > 20);
  const errors = validateOmegaConfiguration({ SMTP_PASSWORD: "use-secret-manager-super-secret" });
  assert.ok(errors.some(x => x.startsWith("SMTP_PASSWORD:")));
  assert.ok(errors.every(x => !x.includes("super-secret")));
});

test("rejects development adapters, short keys and insecure transport", () => {
  const errors = validateOmegaConfiguration({ ASPNETCORE_ENVIRONMENT: "Development",
    JWT_SIGNING_KEY: "short", ENCRYPTION_KEY: "bad", SMTP_USE_TLS: "false",
    OBJECT_STORAGE_ENDPOINT: "http://storage.internal", VIRUS_SCANNER_ENDPOINT: "https://user:password@scanner.internal" });
  for (const prefix of ["ASPNETCORE_ENVIRONMENT:", "JWT_SIGNING_KEY:", "ENCRYPTION_KEY:",
    "SMTP_USE_TLS:", "OBJECT_STORAGE_ENDPOINT:", "VIRUS_SCANNER_ENDPOINT:"])
    assert.ok(errors.some(x => x.startsWith(prefix)), prefix);
});

test("accepts a complete configuration without treating it as operational approval", () => {
  const env = {
    ASPNETCORE_ENVIRONMENT: "Production", DATABASE_PROVIDER: "postgres",
    DISTRIBUTED_LOCK_PROVIDER: "redis", EMAIL_PROVIDER: "smtp", CAPTCHA_PROVIDER: "turnstile",
    OBJECT_STORAGE_PROVIDER: "s3", VIRUS_SCANNER_PROVIDER: "http", DOMAIN_VERIFICATION_PROVIDER: "dns",
    DATABASE_CONNECTION_STRING: "Host=db.internal;Database=one;Username=one;Password=test-only",
    REDIS_CONNECTION_STRING: "redis.internal:6379", EMAIL_FROM_ADDRESS: "test@omega.test",
    SMTP_HOST: "smtp.internal", SMTP_USERNAME: "test-user", SMTP_PASSWORD: "test-only-password",
    TURNSTILE_SITE_KEY: "test-site-key", TURNSTILE_SECRET_KEY: "test-secret-key",
    OBJECT_STORAGE_ENDPOINT: "https://storage.internal", OBJECT_STORAGE_BUCKET: "test-bucket",
    VIRUS_SCANNER_ENDPOINT: "https://scanner.internal", PLATFORM_BASE_DOMAIN: "omega.test",
    PLATFORM_AUTH_DOMAIN: "auth.omega.test", JWT_SIGNING_KEY: "test-only-signing-key-with-at-least-32-bytes",
    ENCRYPTION_KEY: Buffer.alloc(32, 7).toString("base64"), FORWARDED_HEADERS_KNOWN_PROXIES: "127.0.0.1"
  };
  for (const key of ["GOOGLE_CLIENT_ID", "GOOGLE_CLIENT_SECRET", "APPLE_CLIENT_ID", "APPLE_TEAM_ID",
    "APPLE_KEY_ID", "APPLE_PRIVATE_KEY", "FACEBOOK_CLIENT_ID", "FACEBOOK_CLIENT_SECRET"])
    env[key] = "test-only-provider-value";
  assert.deepEqual(validateOmegaConfiguration(env), []);
});
