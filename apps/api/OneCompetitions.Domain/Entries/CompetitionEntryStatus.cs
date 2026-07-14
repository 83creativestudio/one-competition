namespace OneCompetitions.Domain.Entries;

public enum CompetitionEntryStatus
{
    Started,
    Submitted,
    EmailVerificationPending,
    PhoneVerificationPending,
    UnderReview,
    Approved,
    Rejected,
    Duplicate,
    Disqualified,
    Withdrawn,
    Winner,
    ReserveWinner
}
