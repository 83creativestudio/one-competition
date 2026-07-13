namespace OneCompetitions.Contracts.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<AuthTenantResponse> Tenants);

public sealed record AuthTenantResponse(
    Guid TenantId,
    string Name,
    string Slug,
    string Role);
