namespace OneCompetitions.Contracts.Competitions;

public sealed record CompetitionResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string Slug,
    string Status,
    string CompetitionType,
    string DefaultLanguage,
    string TimeZone,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? EntryLimit,
    int PerParticipantEntryLimit,
    int NumberOfWinners,
    int NumberOfReserveWinners,
    bool RequiresManualApproval,
    int? MinimumAge,
    IReadOnlyList<string> AllowedCountryCodes,
    bool RequiresEmailVerification,
    bool RequiresPhoneVerification,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ClosedAt);
