namespace OneCompetitions.Contracts.Entries;

public sealed record SubmitEntryRequest(
    string? Email,
    string? Phone,
    string? FirstName,
    string? LastName,
    string? PreferredLanguage,
    string? IdempotencyKey,
    Guid? CampaignSourceId,
    IReadOnlyList<EntryAnswerRequest> Answers,
    IReadOnlyList<ConsentAcceptanceRequest> Consents,
    string? CaptchaToken = null,
    string? DeviceFingerprint = null,
    DateTimeOffset? FormStartedAt = null,
    DateOnly? DateOfBirth = null,
    string? CountryCode = null);
