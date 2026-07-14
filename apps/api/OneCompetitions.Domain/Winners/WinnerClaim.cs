namespace OneCompetitions.Domain.Winners;

public sealed class WinnerClaim
{
    public Guid Id { get; set; }
    public Guid DrawResultId { get; set; }
    public Guid EntryId { get; set; }
    public WinnerClaimStatus Status { get; set; } = WinnerClaimStatus.Selected;
    public DateTimeOffset? ContactDeadline { get; set; }
    public DateTimeOffset? ClaimDeadline { get; set; }
    public DateTimeOffset? FirstContactedAt { get; set; }
    public DateTimeOffset? LastContactedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public DateTimeOffset? DisqualifiedAt { get; set; }
    public string? DisqualificationReason { get; set; }
    public DateTimeOffset? PrizeDeliveredAt { get; set; }
    public DateTimeOffset? AnnouncementConsentAt { get; set; }
    public Guid? AssignedStaffUserId { get; set; }
    public Guid? ReplacedByWinnerClaimId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
