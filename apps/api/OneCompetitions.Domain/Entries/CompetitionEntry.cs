using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Entries;

public sealed class CompetitionEntry : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid CompetitionId { get; set; }
    public Guid ParticipantId { get; set; }
    public string EntryReference { get; set; } = string.Empty;
    public CompetitionEntryStatus Status { get; set; } = CompetitionEntryStatus.Started;
    public EligibilityStatus EligibilityStatus { get; set; } = EligibilityStatus.Unknown;
    public int RiskScore { get; set; }
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    public Guid? EntrySourceId { get; set; }
    public string? ReferralCodeUsed { get; set; }
    public Guid? ReferredByEntryId { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public string? RejectedReason { get; set; }
    public string? IpHash { get; set; }
    public string? UserAgentHash { get; set; }
    public string? DeviceFingerprintHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
