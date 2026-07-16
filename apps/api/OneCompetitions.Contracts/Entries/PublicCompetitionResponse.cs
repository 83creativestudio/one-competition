using OneCompetitions.Contracts.Competitions;

namespace OneCompetitions.Contracts.Entries;

public sealed record PublicCompetitionResponse(
    Guid CompetitionId,
    Guid TenantId,
    string TenantSlug,
    string Name,
    string Slug,
    string Status,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? MinimumAge,
    IReadOnlyList<string> AllowedCountryCodes,
    bool RequiresEmailVerification,
    bool RequiresPhoneVerification,
    IReadOnlyList<CompetitionFieldResponse> Fields,
    IReadOnlyList<PublicConsentResponse> Consents,
    CompetitionPageResponse? Page);
