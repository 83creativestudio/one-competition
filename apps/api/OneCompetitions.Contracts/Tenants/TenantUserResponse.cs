namespace OneCompetitions.Contracts.Tenants;

public sealed record TenantUserResponse(
    Guid Id,
    Guid UserId,
    string Email,
    string Role,
    string Status,
    DateTimeOffset CreatedAt);
