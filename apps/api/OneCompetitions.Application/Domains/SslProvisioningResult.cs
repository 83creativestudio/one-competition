namespace OneCompetitions.Application.Domains;

public sealed record SslProvisioningResult(
    bool IsProvisioned,
    string? FailureReason,
    DateTimeOffset? CertificateExpiresAt);
