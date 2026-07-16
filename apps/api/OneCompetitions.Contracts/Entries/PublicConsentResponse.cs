namespace OneCompetitions.Contracts.Entries;

public sealed record PublicConsentResponse(Guid Id, string ConsentType, string LanguageCode, string Text, bool IsRequired, int Version);
