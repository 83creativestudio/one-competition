namespace OneCompetitions.Domain.Winners;

public enum WinnerClaimStatus
{
    Selected,
    ContactPending,
    Contacted,
    IdentityVerificationPending,
    EligibilityVerificationPending,
    Accepted,
    PrizeDeliveryPending,
    PrizeDelivered,
    AnnouncementApproved,
    Disqualified,
    Declined,
    Expired,
    Replaced
}
