namespace OneCompetitions.Contracts.Competitions;

public sealed record UpsertCompetitionPageRequest(
    string LanguageCode,
    string Title,
    string? SeoTitle,
    string? SeoDescription,
    string LayoutJson);
