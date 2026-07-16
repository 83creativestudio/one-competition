# Entry Load Test

Run against a disposable staging competition configured for guest entry and the expected test CAPTCHA token:

```bash
BASE_URL=https://competitions.staging.example.com \
CAMPAIGN_HOST=tenant.competitions.staging.example.com \
COMPETITION_SLUG=load-test \
CAPTCHA_TOKEN=provider-test-token \
k6 run tests/load/entry-submission.js
```

The scenario ramps to 25 submissions per second for three minutes and fails when request errors reach 1%, checks fall below 99%, or p95 latency exceeds 750 ms. Use unique staging data and delete it after retaining aggregate evidence. Do not point this test at production.
