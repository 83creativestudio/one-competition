# Development Setup

1. Install .NET SDK 10.0.x and Node.js 22.x.
2. Copy `.env.example` to `.env`.
3. Set a strong `JWT_SIGNING_KEY`.
4. Set `CAPTCHA_PROVIDER=development` and choose a non-production `DEVELOPMENT_CAPTCHA_TOKEN`; the supplied local UI defaults to `development-pass`.
5. Start PostgreSQL and Redis.
6. Run `dotnet restore OneCompetitions.slnx`.
7. Run `npm ci`.
8. Run the API with `dotnet run --project apps/api/OneCompetitions.Api/OneCompetitions.Api.csproj`.
9. Run the web app with `npm --workspace apps/web run dev`.

Development seed data creates a platform administrator and sample tenants. Do not use development fallback secrets in production.

The development domain verification provider verifies `.test`, `.local`, or hostnames containing `verified`. This is for local and automated testing only.

Development email and SMS providers write sanitized delivery events to structured logs. Queued messages are processed by `OneCompetitions.Worker`; run the worker alongside the API when testing verification, invitations, password reset, winner contact, exports, retention, or webhook retries.
