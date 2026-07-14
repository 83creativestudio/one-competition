namespace OneCompetitions.Domain.Winners;

public sealed class WinnerContactAttempt
{
    public Guid Id { get; set; }
    public Guid WinnerClaimId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string RecipientMasked { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid AttemptedByUserId { get; set; }
    public DateTimeOffset AttemptedAt { get; set; }
}
