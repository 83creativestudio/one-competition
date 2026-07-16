FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish apps/api/OneCompetitions.Api/OneCompetitions.Api.csproj -c Release -o /app/publish
RUN dotnet tool restore && ASPNETCORE_ENVIRONMENT=Development dotnet ef migrations bundle --project apps/api/OneCompetitions.Infrastructure --startup-project apps/api/OneCompetitions.Api -o /app/migrations/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends fonts-dejavu-core curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
COPY --from=build /app/migrations/efbundle /app/efbundle
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --retries=3 CMD curl --fail http://localhost:8080/health/live || exit 1
USER $APP_UID
ENTRYPOINT ["dotnet", "OneCompetitions.Api.dll"]
