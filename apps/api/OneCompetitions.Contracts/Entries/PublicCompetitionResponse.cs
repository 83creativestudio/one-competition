using OneCompetitions.Contracts.Competitions;
using OneCompetitions.Contracts.SocialAuth;

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
    bool AllowEmailEntry,
    IReadOnlyList<SocialAuthProviderResponse> SocialAuthProviders,
    IReadOnlyList<SocialActionRequirementResponse> SocialActions,
    IReadOnlyList<CompetitionFieldResponse> Fields,
    IReadOnlyList<PublicConsentResponse> Consents,
    CompetitionPageResponse? Page);
