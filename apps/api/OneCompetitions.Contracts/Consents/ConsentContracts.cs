namespace OneCompetitions.Contracts.Consents;

public sealed record ConsentDefinitionResponse(Guid Id, Guid CompetitionId, string ConsentType, string LanguageCode, string Text, bool IsRequired, int Version, DateTimeOffset CreatedAt);
public sealed record CreateConsentDefinitionRequest(string ConsentType, string LanguageCode, string Text, bool IsRequired);
