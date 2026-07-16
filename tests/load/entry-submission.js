import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  scenarios: { sustained_entries: { executor: "ramping-arrival-rate", startRate: 5, timeUnit: "1s", preAllocatedVUs: 20, maxVUs: 100,
    stages: [{ target: 25, duration: "1m" }, { target: 25, duration: "3m" }, { target: 0, duration: "30s" }] } },
  thresholds: { http_req_failed: ["rate<0.01"], http_req_duration: ["p(95)<750"], checks: ["rate>0.99"] }
};

const baseUrl = __ENV.BASE_URL;
const host = __ENV.CAMPAIGN_HOST;
const slug = __ENV.COMPETITION_SLUG;
const captcha = __ENV.CAPTCHA_TOKEN;

export default function () {
  const id = `${__VU}-${__ITER}-${Date.now()}`;
  const response = http.post(`${baseUrl}/api/public/competitions/${slug}/entries`, JSON.stringify({
    email: `load-${id}@example.test`, firstName: "Load", lastName: "Test", preferredLanguage: "en",
    idempotencyKey: id, deviceFingerprint: `k6-${__VU}`, formStartedAt: new Date(Date.now() - 5000).toISOString(), captchaToken: captcha,
    answers: [], consents: []
  }), { headers: { "Content-Type": "application/json", Host: host } });
  check(response, { "entry accepted": r => r.status === 200 });
  sleep(0.1);
}
