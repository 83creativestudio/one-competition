namespace OneCompetitions.Application.Domains;

public sealed record DomainVerificationResult(
    bool IsVerified,
    string? FailureReason,
    string? ExpectedDnsTarget);
