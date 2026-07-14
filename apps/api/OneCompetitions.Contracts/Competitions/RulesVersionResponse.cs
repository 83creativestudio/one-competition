namespace OneCompetitions.Contracts.Competitions;

public sealed record RulesVersionResponse(
    Guid Id,
    Guid CompetitionId,
    int VersionNumber,
    string LanguageCode,
    string Title,
    string Content,
    string ContentHash,
    DateTimeOffset EffectiveAt,
    DateTimeOffset CreatedAt);
