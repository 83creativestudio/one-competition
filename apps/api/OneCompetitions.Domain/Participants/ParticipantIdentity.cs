namespace OneCompetitions.Domain.Participants;

public sealed class ParticipantIdentity
{
    public Guid Id { get; set; }
    public Guid ParticipantId { get; set; }
    public Participant Participant { get; set; } = null!;
    public string Provider { get; set; } = string.Empty;
    public string ProviderSubjectHash { get; set; } = string.Empty;
    public string? ProviderEmail { get; set; }
    public bool IsVerified { get; set; }
    public DateTimeOffset ConnectedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
