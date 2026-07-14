namespace OneCompetitions.Domain.Competitions;

public sealed class CompetitionPage
{
    public Guid Id { get; set; }
    public Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;
    public string LanguageCode { get; set; } = "en";
    public string Status { get; set; } = "Draft";
    public string Title { get; set; } = string.Empty;
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public Guid? OpenGraphAssetId { get; set; }
    public string LayoutJson { get; set; } = "{\"schemaVersion\":1,\"blocks\":[]}";
    public int PublishedVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
