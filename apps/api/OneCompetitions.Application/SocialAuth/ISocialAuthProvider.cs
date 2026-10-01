namespace OneCompetitions.Application.SocialAuth;

public sealed record SocialAuthorizationRequest(string RedirectUri, string State, string Nonce, string PkceChallenge);
public sealed record SocialTokenExchangeRequest(string Code, string RedirectUri, string PkceVerifier, string Nonce);
public sealed record SocialIdentityProfile(
    string Subject,
    string? Email,
    bool EmailVerified,
    string? DisplayName,
    string? UserName,
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset? TokenExpiresAt,
    IReadOnlyList<string> GrantedScopes);
public sealed record SocialActionVerificationRequest(string Subject, string AccessToken, string ActionType, string TargetReference);
public sealed record SocialActionVerificationResult(string Status, string? EvidenceJson);

public interface ISocialAuthProvider
{
    string Name { get; }
    string DisplayName { get; }
    bool IsConfigured { get; }
    IReadOnlySet<string> SupportedActions { get; }
    Uri CreateAuthorizationUri(SocialAuthorizationRequest request);
    Task<SocialIdentityProfile> ExchangeAsync(SocialTokenExchangeRequest request, CancellationToken cancellationToken);
    Task<SocialActionVerificationResult> VerifyActionAsync(SocialActionVerificationRequest request, CancellationToken cancellationToken);
}

public interface ISocialAuthProviderRegistry
{
    ISocialAuthProvider Get(string provider);
    IReadOnlyList<ISocialAuthProvider> All { get; }
}
