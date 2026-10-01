using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Participants;

public sealed class ParticipantSocialActionVerification : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid CompetitionId { get; set; }
    public Guid RequirementId { get; set; }
    public Guid ParticipantId { get; set; }
    public Guid ParticipantIdentityId { get; set; }
    public string Status { get; set; } = "Pending";
    public string? EvidenceJson { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
