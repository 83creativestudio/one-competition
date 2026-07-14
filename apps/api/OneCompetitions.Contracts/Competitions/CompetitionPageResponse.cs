namespace OneCompetitions.Contracts.Competitions;

public sealed record CompetitionPageResponse(
    Guid Id,
    Guid CompetitionId,
    string LanguageCode,
    string Status,
    string Title,
    string? SeoTitle,
    string? SeoDescription,
    string LayoutJson,
    int PublishedVersion,
    DateTimeOffset? PublishedAt);
