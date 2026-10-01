using OneCompetitions.Contracts.SocialAuth;

namespace OneCompetitions.Application.SocialAuth;

public sealed record SocialAuthStartResult(Uri AuthorizationUri);
public sealed record SocialAuthCallbackResult(Uri ReturnUri);

public interface ISocialAuthService
{
    Task<IReadOnlyList<SocialAuthProviderResponse>> ProvidersAsync(string competitionSlug, CancellationToken cancellationToken);
    Task<SocialAuthStartResult> StartAsync(string competitionSlug, string provider, string returnUrl, CancellationToken cancellationToken);
    Task<SocialAuthCallbackResult> CompleteProviderCallbackAsync(string provider, string? code, string state, string? error, CancellationToken cancellationToken);
    Task<SocialAuthSessionResponse> ExchangeCompletionAsync(CompleteSocialAuthRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<SocialActionRequirementResponse>> ListRequirementsAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken);
    Task<SocialActionRequirementResponse> AddRequirementAsync(Guid userId, Guid competitionId, UpsertSocialActionRequirementRequest request, CancellationToken cancellationToken);
    Task DeleteRequirementAsync(Guid userId, Guid competitionId, Guid requirementId, CancellationToken cancellationToken);
    Task<SocialActionVerificationResponse> VerifyActionAsync(Guid requirementId, string participantSessionToken, CancellationToken cancellationToken);
}
