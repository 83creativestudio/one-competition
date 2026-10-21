import assert from "node:assert/strict";
import test from "node:test";
import { isAllowedRequestOrigin } from "../lib/request-origin.ts";
const { Headers } = globalThis;

test("accepts the public proxy host despite an internal Next.js hostname", () => {
  const headers = new Headers({ origin: "https://competition.onedigital.com.cy", host: "competition.onedigital.com.cy" });
  assert.equal(isAllowedRequestOrigin(headers, "web:3000"), true);
});

test("rejects foreign origins even when forwarded-host is spoofed", () => {
  const headers = new Headers({ origin: "https://attacker.test", host: "competition.onedigital.com.cy", "x-forwarded-host": "attacker.test" });
  assert.equal(isAllowedRequestOrigin(headers, "web:3000"), false);
});

test("retains port checks for local development", () => {
  assert.equal(isAllowedRequestOrigin(new Headers({ origin: "http://localhost:3002", host: "localhost:3002" }), "localhost:3000"), true);
  assert.equal(isAllowedRequestOrigin(new Headers({ origin: "http://localhost:3001", host: "localhost:3002" }), "localhost:3000"), false);
});

test("rejects malformed and opaque origins without throwing", () => {
  for (const origin of ["null", "not-a-url", "https://user@competition.onedigital.com.cy", "https://competition.onedigital.com.cy/path"]) {
    assert.equal(isAllowedRequestOrigin(new Headers({ origin, host: "competition.onedigital.com.cy" }), "web:3000"), false);
  }
});

test("honours required Origin and browser cross-site metadata", () => {
  assert.equal(isAllowedRequestOrigin(new Headers(), "localhost:3002", true), false);
  assert.equal(isAllowedRequestOrigin(new Headers(), "localhost:3002"), true);
  assert.equal(isAllowedRequestOrigin(new Headers({ origin: "https://competition.onedigital.com.cy", host: "competition.onedigital.com.cy", "sec-fetch-site": "cross-site" }), "web:3000"), false);
});
