namespace OneCompetitions.Domain.Draws;

public sealed class DrawResult
{
    public Guid Id { get; set; }
    public Guid DrawId { get; set; }
    public Draw Draw { get; set; } = null!;
    public Guid EntryId { get; set; }
    public DrawResultType ResultType { get; set; }
    public int Position { get; set; }
    public DateTimeOffset SelectedAt { get; set; }
    public string SelectionProofHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
