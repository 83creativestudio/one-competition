namespace OneCompetitions.Domain.Fraud;

public sealed class EntryReview
{
    public Guid Id { get; set; }
    public Guid EntryId { get; set; }
    public Guid ReviewerUserId { get; set; }
    public EntryReviewDecision Decision { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
