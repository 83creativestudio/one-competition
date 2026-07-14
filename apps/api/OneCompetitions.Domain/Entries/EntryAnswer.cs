namespace OneCompetitions.Domain.Entries;

public sealed class EntryAnswer
{
    public Guid Id { get; set; }
    public Guid EntryId { get; set; }
    public CompetitionEntry Entry { get; set; } = null!;
    public Guid CompetitionFieldId { get; set; }
    public string? StringValue { get; set; }
    public decimal? NumberValue { get; set; }
    public DateOnly? DateValue { get; set; }
    public bool? BooleanValue { get; set; }
    public string? JsonValue { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
