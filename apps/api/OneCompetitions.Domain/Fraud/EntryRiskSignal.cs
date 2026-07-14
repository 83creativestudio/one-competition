namespace OneCompetitions.Domain.Fraud;

public sealed class EntryRiskSignal
{
    public Guid Id { get; set; }
    public Guid EntryId { get; set; }
    public Guid? RuleId { get; set; }
    public string SignalType { get; set; } = string.Empty;
    public int ScoreImpact { get; set; }
    public string Description { get; set; } = string.Empty;
    public string EvidenceJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
}
