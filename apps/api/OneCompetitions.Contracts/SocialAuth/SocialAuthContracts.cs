namespace OneCompetitions.Contracts.SocialAuth;

public sealed record SocialAuthProviderResponse(string Provider, string DisplayName, bool IsConfigured, IReadOnlyList<string> SupportedActions);
public sealed record CompleteSocialAuthRequest(string Code);
public sealed record SocialAuthSessionResponse(
    string Provider,
    string? Email,
    string? DisplayName,
    string? UserName,
    IReadOnlyList<SocialActionRequirementResponse> Actions,
    string ParticipantSessionToken);
public sealed record SocialActionRequirementResponse(
    Guid Id,
    string Provider,
    string ActionType,
    string TargetReference,
    string? Description,
    bool IsRequired,
    bool SupportsAutomatedVerification,
    string? Status = null);
public sealed record UpsertSocialActionRequirementRequest(string Provider, string ActionType, string TargetReference, string? Description, bool IsRequired);
public sealed record SocialActionVerificationResponse(Guid RequirementId, string Status, string? EvidenceJson, DateTimeOffset? VerifiedAt);
