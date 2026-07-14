namespace OneCompetitions.Contracts.Domains;

public sealed record TenantDomainResponse(
    Guid Id,
    Guid TenantId,
    string Hostname,
    string DomainType,
    string Status,
    bool IsPrimary,
    string? VerificationMethod,
    string? VerificationToken,
    string? ExpectedDnsTarget,
    DateTimeOffset? VerifiedAt,
    DateTimeOffset? LastCheckedAt,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
