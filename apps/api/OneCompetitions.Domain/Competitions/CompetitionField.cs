namespace OneCompetitions.Domain.Competitions;

public sealed class CompetitionField
{
    public Guid Id { get; set; }
    public Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;
    public string FieldKey { get; set; } = string.Empty;
    public CompetitionFieldType FieldType { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public string ValidationJson { get; set; } = "{}";
    public string OptionsJson { get; set; } = "{}";
    public bool IsSensitive { get; set; }
    public bool IsSearchable { get; set; }
    public bool IsExportable { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
