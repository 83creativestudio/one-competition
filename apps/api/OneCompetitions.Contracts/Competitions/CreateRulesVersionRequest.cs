namespace OneCompetitions.Contracts.Competitions;

public sealed record CreateRulesVersionRequest(string LanguageCode, string Title, string Content, DateTimeOffset? EffectiveAt);
