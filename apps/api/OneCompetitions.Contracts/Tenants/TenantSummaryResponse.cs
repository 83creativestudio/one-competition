namespace OneCompetitions.Contracts.Tenants;

public sealed record TenantSummaryResponse(
    Guid Id,
    string Name,
    string Slug,
    string Status,
    int ActiveUserCount,
    DateTimeOffset CreatedAt);
