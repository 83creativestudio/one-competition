using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Draws;

public sealed class DrawCertificate : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid DrawId { get; set; }
    public Draw Draw { get; set; } = null!;
    public string Status { get; set; } = "Pending";
    public string? StorageKey { get; set; }
    public string? Sha256Hash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? GeneratedAt { get; set; }
    public string? FailureReason { get; set; }
}
