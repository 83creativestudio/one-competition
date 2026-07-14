namespace OneCompetitions.Domain.Draws;

public sealed class DrawEntrySnapshot
{
    public Guid Id { get; set; }
    public Guid DrawId { get; set; }
    public Draw Draw { get; set; } = null!;
    public Guid EntryId { get; set; }
    public string EntryReference { get; set; } = string.Empty;
    public string ParticipantReferenceHash { get; set; } = string.Empty;
    public int Weight { get; set; } = 1;
    public bool Included { get; set; }
    public string? ExclusionReason { get; set; }
    public string SnapshotHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
