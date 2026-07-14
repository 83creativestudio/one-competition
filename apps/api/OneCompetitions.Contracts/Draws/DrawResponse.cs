namespace OneCompetitions.Contracts.Draws;

public sealed record DrawResponse(
    Guid Id,
    Guid CompetitionId,
    string Status,
    string DrawReference,
    int RequestedWinnerCount,
    int RequestedReserveCount,
    int EligibleEntryCount,
    int ExcludedEntryCount,
    string EntryPoolHash,
    string ConfigurationHash,
    DateTimeOffset? PreparedAt,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? ExecutedAt);
