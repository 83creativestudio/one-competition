using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Tenants;

public sealed class TenantDomain : TenantScopedEntity
{
    public Guid Id { get; set; }
    public required string Hostname { get; set; }
    public TenantDomainType DomainType { get; set; }
    public TenantDomainStatus Status { get; set; } = TenantDomainStatus.Pending;
    public bool IsPrimary { get; set; }
    public string? VerificationMethod { get; set; }
    public string? VerificationToken { get; set; }
    public string? ExpectedDnsTarget { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset? LastCheckedAt { get; set; }
    public string? SslStatus { get; set; }
    public DateTimeOffset? SslProvisionedAt { get; set; }
    public DateTimeOffset? CertificateExpiresAt { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Tenant? Tenant { get; set; }
}
