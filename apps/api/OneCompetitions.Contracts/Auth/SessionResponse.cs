namespace OneCompetitions.Contracts.Auth;

public sealed record SessionResponse(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    string? IpAddress,
    string? UserAgent);
