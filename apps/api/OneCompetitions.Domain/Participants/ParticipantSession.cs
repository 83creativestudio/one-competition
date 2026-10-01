namespace OneCompetitions.Domain.Participants;

public sealed class ParticipantSession
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompetitionId { get; set; }
    public Guid ParticipantId { get; set; }
    public Guid ParticipantIdentityId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public Guid? EntryId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
