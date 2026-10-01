using OneCompetitions.Application.SocialAuth;

namespace OneCompetitions.Api.IntegrationTests;

internal sealed class FakeSocialAuthProvider(string name, IReadOnlySet<string>? actions = null) : ISocialAuthProvider
{
    public string Name { get; } = name;
    public string DisplayName => Name;
    public bool IsConfigured => true;
    public IReadOnlySet<string> SupportedActions { get; } = actions ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public Uri CreateAuthorizationUri(SocialAuthorizationRequest request) =>
        new($"https://provider.test/{Name.ToLowerInvariant()}/authorize?state={Uri.EscapeDataString(request.State)}&redirect_uri={Uri.EscapeDataString(request.RedirectUri)}&nonce={Uri.EscapeDataString(request.Nonce)}&code_challenge={Uri.EscapeDataString(request.PkceChallenge)}");

    public Task<SocialIdentityProfile> ExchangeAsync(SocialTokenExchangeRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new SocialIdentityProfile($"{Name.ToLowerInvariant()}-{request.Code}", $"{request.Code}@social.test", true,
            $"{Name} Participant", $"{Name.ToLowerInvariant()}_participant", $"access-{request.Code}", $"refresh-{request.Code}",
            DateTimeOffset.UtcNow.AddHours(1), ["profile", "email"]));

    public Task<SocialActionVerificationResult> VerifyActionAsync(SocialActionVerificationRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(SupportedActions.Contains(request.ActionType)
            ? new SocialActionVerificationResult("Verified", "{\"source\":\"test-provider\"}")
            : new SocialActionVerificationResult("Unsupported", null));
}

internal sealed class FakeSocialAuthProviderRegistry : ISocialAuthProviderRegistry
{
    public IReadOnlyList<ISocialAuthProvider> All { get; } =
    [
        new FakeSocialAuthProvider("Google"),
        new FakeSocialAuthProvider("Apple"),
        new FakeSocialAuthProvider("Facebook"),
        new FakeSocialAuthProvider("X", new HashSet<string>(["Follow", "Like"], StringComparer.OrdinalIgnoreCase)),
        new FakeSocialAuthProvider("TikTok")
    ];

    public ISocialAuthProvider Get(string provider) => All.SingleOrDefault(x => x.Name.Equals(provider, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException("Provider not found.");
}
