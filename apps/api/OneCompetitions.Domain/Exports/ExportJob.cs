using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Exports;

public sealed class ExportJob : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid? CompetitionId { get; set; }
    public string ExportType { get; set; } = string.Empty;
    public string Format { get; set; } = "Csv";
    public string Status { get; set; } = "Pending";
    public Guid RequestedByUserId { get; set; }
    public string? StorageKey { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
}
