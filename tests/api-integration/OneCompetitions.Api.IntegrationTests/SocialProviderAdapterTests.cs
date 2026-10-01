using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using OneCompetitions.Application.SocialAuth;
using OneCompetitions.Infrastructure.Services;

namespace OneCompetitions.Api.IntegrationTests;

public sealed class SocialProviderAdapterTests
{
    [Fact]
    public void Production_adapters_use_the_central_callback_contract()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GOOGLE_CLIENT_ID"] = "google-id", ["GOOGLE_CLIENT_SECRET"] = "google-secret",
            ["APPLE_CLIENT_ID"] = "apple-id", ["APPLE_TEAM_ID"] = "apple-team", ["APPLE_KEY_ID"] = "apple-key", ["APPLE_PRIVATE_KEY"] = "configured-at-exchange",
            ["FACEBOOK_CLIENT_ID"] = "facebook-id", ["FACEBOOK_CLIENT_SECRET"] = "facebook-secret",
            ["X_CLIENT_ID"] = "x-id", ["X_CLIENT_SECRET"] = "x-secret",
            ["TIKTOK_CLIENT_KEY"] = "tiktok-key", ["TIKTOK_CLIENT_SECRET"] = "tiktok-secret"
        }).Build();
        using var client = new HttpClient();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        ISocialAuthProvider[] providers =
        [
            new GoogleSocialAuthProvider(client, configuration, cache),
            new AppleSocialAuthProvider(client, configuration, cache),
            new FacebookSocialAuthProvider(client, configuration),
            new XSocialAuthProvider(client, configuration),
            new TikTokSocialAuthProvider(client, configuration)
        ];
        var request = new SocialAuthorizationRequest("https://auth.example.test/api/participant-auth/provider/callback", "state", "nonce", "challenge");

        Assert.All(providers, provider => Assert.True(provider.IsConfigured));
        Assert.Equal(["Apple", "Facebook", "Google", "TikTok", "X"], providers.Select(x => x.Name).Order().ToArray());
        Assert.All(providers, provider => Assert.Contains("state=state", provider.CreateAuthorizationUri(request).Query));
        Assert.Contains("code_challenge=challenge", providers.Single(x => x.Name == "Google").CreateAuthorizationUri(request).Query);
        Assert.Contains("code_challenge=challenge", providers.Single(x => x.Name == "X").CreateAuthorizationUri(request).Query);
        Assert.Equal(new HashSet<string>(["Follow", "Like"]), providers.Single(x => x.Name == "X").SupportedActions);
        Assert.All(providers.Where(x => x.Name != "X"), provider => Assert.Empty(provider.SupportedActions));
    }
}
