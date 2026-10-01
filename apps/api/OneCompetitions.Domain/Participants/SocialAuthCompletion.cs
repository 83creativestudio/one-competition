namespace OneCompetitions.Domain.Participants;

public sealed class SocialAuthCompletion
{
    public Guid Id { get; set; }
    public Guid AuthTransactionId { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompetitionId { get; set; }
    public Guid ParticipantId { get; set; }
    public Guid ParticipantIdentityId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
