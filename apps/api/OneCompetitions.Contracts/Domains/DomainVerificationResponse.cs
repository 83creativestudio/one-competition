namespace OneCompetitions.Contracts.Domains;

public sealed record DomainVerificationResponse(
    Guid DomainId,
    string Hostname,
    string Status,
    bool IsVerified,
    string? FailureReason,
    DateTimeOffset CheckedAt);
