using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Draws;

public sealed class Draw : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid CompetitionId { get; set; }
    public DrawStatus Status { get; set; } = DrawStatus.Draft;
    public string DrawReference { get; set; } = string.Empty;
    public string AlgorithmVersion { get; set; } = "secure-random-v1";
    public int RequestedWinnerCount { get; set; }
    public int RequestedReserveCount { get; set; }
    public int EligibleEntryCount { get; set; }
    public int ExcludedEntryCount { get; set; }
    public string EntryPoolHash { get; set; } = string.Empty;
    public string ConfigurationHash { get; set; } = string.Empty;
    public Guid PreparedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public Guid? ExecutedByUserId { get; set; }
    public DateTimeOffset? PreparedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? ExecutedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
