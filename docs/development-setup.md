# Development Setup

1. Install .NET SDK 10.0.x and Node.js 22.x.
2. Copy `.env.example` to `.env`.
3. Set a strong `JWT_SIGNING_KEY`.
4. Start PostgreSQL and Redis.
5. Run `dotnet restore OneCompetitions.slnx`.
6. Run `npm ci`.
7. Run the API with `dotnet run --project apps/api/OneCompetitions.Api/OneCompetitions.Api.csproj`.
8. Run the web app with `npm --workspace apps/web run dev`.

Development seed data creates a platform administrator and sample tenants. Do not use development fallback secrets in production.
