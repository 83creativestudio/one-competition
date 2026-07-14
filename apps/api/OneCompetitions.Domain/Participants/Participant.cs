using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Participants;

public sealed class Participant : TenantScopedEntity
{
    public Guid Id { get; set; }
    public string? PrimaryEmail { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? CountryCode { get; set; }
    public string? City { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public ParticipantStatus Status { get; set; } = ParticipantStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? AnonymisedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
