namespace OneCompetitions.Domain.Consents;

public sealed class ParticipantConsent
{
    public Guid Id { get; set; }
    public Guid ParticipantId { get; set; }
    public Guid EntryId { get; set; }
    public Guid ConsentDefinitionId { get; set; }
    public bool Accepted { get; set; }
    public string ConsentTextHash { get; set; } = string.Empty;
    public string? IpHash { get; set; }
    public string? UserAgentHash { get; set; }
    public DateTimeOffset AcceptedAt { get; set; }
    public DateTimeOffset? WithdrawnAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
