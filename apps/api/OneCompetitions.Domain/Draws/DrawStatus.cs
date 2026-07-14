namespace OneCompetitions.Domain.Draws;

public enum DrawStatus
{
    Draft,
    Preparing,
    Prepared,
    AwaitingApproval,
    Approved,
    Executing,
    Completed,
    Cancelled,
    Invalidated
}
