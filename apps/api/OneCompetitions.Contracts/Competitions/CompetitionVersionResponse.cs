namespace OneCompetitions.Contracts.Competitions;

public sealed record CompetitionVersionResponse(
    Guid Id,
    Guid CompetitionId,
    int VersionNumber,
    string ChangeSummary,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt);
