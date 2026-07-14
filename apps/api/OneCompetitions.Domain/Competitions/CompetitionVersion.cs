namespace OneCompetitions.Domain.Competitions;

public sealed class CompetitionVersion
{
    public Guid Id { get; set; }
    public Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;
    public int VersionNumber { get; set; }
    public string ConfigurationJson { get; set; } = "{}";
    public Guid? RulesVersionId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string ChangeSummary { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
