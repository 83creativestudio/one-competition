namespace OneCompetitions.Domain.Competitions;

public enum CompetitionStatus
{
    Draft,
    InternalReview,
    ClientReview,
    LegalReview,
    Approved,
    Scheduled,
    Live,
    Paused,
    Closed,
    DrawPreparation,
    DrawApproved,
    WinnerVerification,
    Completed,
    Archived,
    Cancelled
}
