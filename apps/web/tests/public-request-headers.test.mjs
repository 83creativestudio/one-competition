import test from "node:test";
import assert from "node:assert/strict";
import { publicRequestHeaders } from "../lib/public-request-headers.ts";
const { Headers } = globalThis;

test("forwards the public authority for server-rendered tenant pages and media", () => {
  assert.deepEqual(publicRequestHeaders(new Headers({ host: "competition.onedigital.com.cy", "x-forwarded-proto": "https" })), {
    "X-Forwarded-Host": "competition.onedigital.com.cy", "X-Forwarded-Proto": "https"
  });
});

test("does not trust an inbound forwarded-host over the actual host", () => {
  const result = publicRequestHeaders(new Headers({ host: "competition.onedigital.com.cy", "x-forwarded-host": "foreign.test" }));
  assert.equal(result["X-Forwarded-Host"], "competition.onedigital.com.cy");
});

test("preserves local ports and uses the caller fallback when host is missing", () => {
  assert.equal(publicRequestHeaders(new Headers({ host: "localhost:3003" }))["X-Forwarded-Host"], "localhost:3003");
  assert.equal(publicRequestHeaders(new Headers(), "localhost:3003")["X-Forwarded-Host"], "localhost:3003");
});
