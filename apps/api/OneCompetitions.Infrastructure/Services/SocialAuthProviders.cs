using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OneCompetitions.Application.SocialAuth;

namespace OneCompetitions.Infrastructure.Services;

public sealed class SocialAuthProviderRegistry(IEnumerable<ISocialAuthProvider> providers) : ISocialAuthProviderRegistry
{
    public IReadOnlyList<ISocialAuthProvider> All { get; } = providers.ToList();
    public ISocialAuthProvider Get(string provider) => All.SingleOrDefault(x => x.Name.Equals(provider, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException("The selected social authentication provider is not supported.");
}

internal static class SocialProviderHelpers
{
    public static Uri AuthorizationUri(string endpoint, IReadOnlyDictionary<string, string?> values) =>
        new(QueryHelpers.AddQueryString(endpoint, values.Where(x => x.Value is not null).ToDictionary(x => x.Key, x => x.Value)));

    public static async Task<JsonDocument> PostFormAsync(HttpClient client, string endpoint, Dictionary<string, string> values, CancellationToken cancellationToken)
    {
        using var response = await client.PostAsync(endpoint, new FormUrlEncodedContent(values), cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"The social provider token exchange failed with HTTP {(int)response.StatusCode}.");
        return JsonDocument.Parse(body);
    }

    public static async Task<ClaimsPrincipal> ValidateIdTokenAsync(HttpClient client, IMemoryCache cache, string token, string jwksUri,
        IEnumerable<string> issuers, string audience, string nonce, CancellationToken cancellationToken)
    {
        var keys = await cache.GetOrCreateAsync($"social-jwks:{jwksUri}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);
            var json = await client.GetStringAsync(jwksUri, cancellationToken);
            return new JsonWebKeySet(json).GetSigningKeys().ToArray();
        }) ?? throw new InvalidOperationException("The social provider signing keys are unavailable.");
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuers = issuers, ValidateAudience = true, ValidAudience = audience,
            ValidateLifetime = true, ValidateIssuerSigningKey = true, IssuerSigningKeys = keys, ClockSkew = TimeSpan.FromMinutes(2),
            NameClaimType = "name"
        }, out _);
        var receivedNonce = principal.FindFirstValue("nonce");
        if (string.IsNullOrWhiteSpace(receivedNonce) || !CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(receivedNonce), System.Text.Encoding.UTF8.GetBytes(nonce)))
            throw new SecurityTokenValidationException("The social authentication nonce is invalid.");
        return principal;
    }

    public static string? String(JsonElement element, string name) => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    public static long? Int64(JsonElement element, string name) => element.TryGetProperty(name, out var value) && value.TryGetInt64(out var result) ? result : null;
}

public abstract class UnsupportedActionSocialProvider : ISocialAuthProvider
{
    public abstract string Name { get; }
    public abstract string DisplayName { get; }
    public abstract bool IsConfigured { get; }
    public virtual IReadOnlySet<string> SupportedActions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public abstract Uri CreateAuthorizationUri(SocialAuthorizationRequest request);
    public abstract Task<SocialIdentityProfile> ExchangeAsync(SocialTokenExchangeRequest request, CancellationToken cancellationToken);
    public virtual Task<SocialActionVerificationResult> VerifyActionAsync(SocialActionVerificationRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new SocialActionVerificationResult("Unsupported", JsonSerializer.Serialize(new { provider = Name, action = request.ActionType })));
}

public sealed class GoogleSocialAuthProvider(HttpClient client, IConfiguration configuration, IMemoryCache cache) : UnsupportedActionSocialProvider
{
    public override string Name => "Google";
    public override string DisplayName => "Google";
    public override bool IsConfigured => !string.IsNullOrWhiteSpace(configuration["GOOGLE_CLIENT_ID"]) && !string.IsNullOrWhiteSpace(configuration["GOOGLE_CLIENT_SECRET"]);
    public override Uri CreateAuthorizationUri(SocialAuthorizationRequest request) => SocialProviderHelpers.AuthorizationUri(
        configuration["GOOGLE_AUTHORIZATION_ENDPOINT"] ?? "https://accounts.google.com/o/oauth2/v2/auth", new Dictionary<string, string?>
        {
            ["client_id"] = configuration["GOOGLE_CLIENT_ID"], ["redirect_uri"] = request.RedirectUri, ["response_type"] = "code",
            ["scope"] = "openid email profile", ["state"] = request.State, ["nonce"] = request.Nonce,
            ["code_challenge"] = request.PkceChallenge, ["code_challenge_method"] = "S256", ["prompt"] = "select_account"
        });
    public override async Task<SocialIdentityProfile> ExchangeAsync(SocialTokenExchangeRequest request, CancellationToken cancellationToken)
    {
        using var json = await SocialProviderHelpers.PostFormAsync(client, configuration["GOOGLE_TOKEN_ENDPOINT"] ?? "https://oauth2.googleapis.com/token", new()
        {
            ["client_id"] = configuration["GOOGLE_CLIENT_ID"]!, ["client_secret"] = configuration["GOOGLE_CLIENT_SECRET"]!,
            ["code"] = request.Code, ["code_verifier"] = request.PkceVerifier, ["redirect_uri"] = request.RedirectUri, ["grant_type"] = "authorization_code"
        }, cancellationToken);
        var root = json.RootElement; var idToken = SocialProviderHelpers.String(root, "id_token") ?? throw new InvalidOperationException("Google did not return an identity token.");
        var principal = await SocialProviderHelpers.ValidateIdTokenAsync(client, cache, idToken, "https://www.googleapis.com/oauth2/v3/certs",
            ["https://accounts.google.com", "accounts.google.com"], configuration["GOOGLE_CLIENT_ID"]!, request.Nonce, cancellationToken);
        return new SocialIdentityProfile(principal.FindFirstValue("sub")!, principal.FindFirstValue("email"), principal.FindFirstValue("email_verified") == "true",
            principal.FindFirstValue("name"), null, SocialProviderHelpers.String(root, "access_token") ?? string.Empty, SocialProviderHelpers.String(root, "refresh_token"),
            DateTimeOffset.UtcNow.AddSeconds(SocialProviderHelpers.Int64(root, "expires_in") ?? 3600), (SocialProviderHelpers.String(root, "scope") ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}

public sealed class AppleSocialAuthProvider(HttpClient client, IConfiguration configuration, IMemoryCache cache) : UnsupportedActionSocialProvider
{
    public override string Name => "Apple";
    public override string DisplayName => "Apple";
    public override bool IsConfigured => new[] { "APPLE_CLIENT_ID", "APPLE_TEAM_ID", "APPLE_KEY_ID", "APPLE_PRIVATE_KEY" }.All(x => !string.IsNullOrWhiteSpace(configuration[x]));
    public override Uri CreateAuthorizationUri(SocialAuthorizationRequest request) => SocialProviderHelpers.AuthorizationUri("https://appleid.apple.com/auth/authorize", new Dictionary<string, string?>
    {
        ["client_id"] = configuration["APPLE_CLIENT_ID"], ["redirect_uri"] = request.RedirectUri, ["response_type"] = "code id_token",
        ["response_mode"] = "form_post", ["scope"] = "name email", ["state"] = request.State, ["nonce"] = request.Nonce
    });
    public override async Task<SocialIdentityProfile> ExchangeAsync(SocialTokenExchangeRequest request, CancellationToken cancellationToken)
    {
        using var json = await SocialProviderHelpers.PostFormAsync(client, "https://appleid.apple.com/auth/token", new()
        {
            ["client_id"] = configuration["APPLE_CLIENT_ID"]!, ["client_secret"] = CreateClientSecret(), ["code"] = request.Code,
            ["redirect_uri"] = request.RedirectUri, ["grant_type"] = "authorization_code"
        }, cancellationToken);
        var root = json.RootElement; var idToken = SocialProviderHelpers.String(root, "id_token") ?? throw new InvalidOperationException("Apple did not return an identity token.");
        var principal = await SocialProviderHelpers.ValidateIdTokenAsync(client, cache, idToken, "https://appleid.apple.com/auth/keys",
            ["https://appleid.apple.com"], configuration["APPLE_CLIENT_ID"]!, request.Nonce, cancellationToken);
        var verified = principal.FindFirstValue("email_verified") is "true" or "True";
        return new SocialIdentityProfile(principal.FindFirstValue("sub")!, principal.FindFirstValue("email"), verified, null, null,
            SocialProviderHelpers.String(root, "access_token") ?? string.Empty, SocialProviderHelpers.String(root, "refresh_token"),
            DateTimeOffset.UtcNow.AddSeconds(SocialProviderHelpers.Int64(root, "expires_in") ?? 3600), ["openid", "email"]);
    }
    private string CreateClientSecret()
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(configuration["APPLE_PRIVATE_KEY"]!.Replace("\\n", "\n", StringComparison.Ordinal));
        var key = new ECDsaSecurityKey(ecdsa) { KeyId = configuration["APPLE_KEY_ID"] };
        var now = DateTimeOffset.UtcNow;
        var token = new JwtSecurityToken(configuration["APPLE_TEAM_ID"], "https://appleid.apple.com",
            [new Claim("sub", configuration["APPLE_CLIENT_ID"]!)], now.UtcDateTime, now.AddMinutes(5).UtcDateTime, new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256));
        token.Header["kid"] = configuration["APPLE_KEY_ID"];
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class FacebookSocialAuthProvider(HttpClient client, IConfiguration configuration) : UnsupportedActionSocialProvider
{
    private string Version => configuration["FACEBOOK_GRAPH_VERSION"] ?? "v23.0";
    public override string Name => "Facebook";
    public override string DisplayName => "Facebook";
    public override bool IsConfigured => !string.IsNullOrWhiteSpace(configuration["FACEBOOK_CLIENT_ID"]) && !string.IsNullOrWhiteSpace(configuration["FACEBOOK_CLIENT_SECRET"]);
    public override Uri CreateAuthorizationUri(SocialAuthorizationRequest request) => SocialProviderHelpers.AuthorizationUri($"https://www.facebook.com/{Version}/dialog/oauth", new Dictionary<string, string?>
    {
        ["client_id"] = configuration["FACEBOOK_CLIENT_ID"], ["redirect_uri"] = request.RedirectUri, ["response_type"] = "code",
        ["scope"] = "public_profile,email", ["state"] = request.State
    });
    public override async Task<SocialIdentityProfile> ExchangeAsync(SocialTokenExchangeRequest request, CancellationToken cancellationToken)
    {
        using var tokenJson = await SocialProviderHelpers.PostFormAsync(client, $"https://graph.facebook.com/{Version}/oauth/access_token", new()
        {
            ["client_id"] = configuration["FACEBOOK_CLIENT_ID"]!, ["client_secret"] = configuration["FACEBOOK_CLIENT_SECRET"]!,
            ["code"] = request.Code, ["redirect_uri"] = request.RedirectUri
        }, cancellationToken);
        var accessToken = SocialProviderHelpers.String(tokenJson.RootElement, "access_token") ?? throw new InvalidOperationException("Facebook did not return an access token.");
        using var profile = await client.GetFromJsonAsync<JsonDocument>(SocialProviderHelpers.AuthorizationUri($"https://graph.facebook.com/{Version}/me", new Dictionary<string, string?>
        { ["fields"] = "id,name,email", ["access_token"] = accessToken }), cancellationToken) ?? throw new InvalidOperationException("Facebook profile lookup failed.");
        var root = profile.RootElement;
        return new SocialIdentityProfile(SocialProviderHelpers.String(root, "id")!, SocialProviderHelpers.String(root, "email"), false,
            SocialProviderHelpers.String(root, "name"), null, accessToken, null,
            DateTimeOffset.UtcNow.AddSeconds(SocialProviderHelpers.Int64(tokenJson.RootElement, "expires_in") ?? 3600), ["public_profile", "email"]);
    }
}

public sealed class XSocialAuthProvider(HttpClient client, IConfiguration configuration) : UnsupportedActionSocialProvider
{
    public override string Name => "X";
    public override string DisplayName => "X";
    public override bool IsConfigured => !string.IsNullOrWhiteSpace(configuration["X_CLIENT_ID"]) && !string.IsNullOrWhiteSpace(configuration["X_CLIENT_SECRET"]);
    public override IReadOnlySet<string> SupportedActions { get; } = new HashSet<string>(["Follow", "Like"], StringComparer.OrdinalIgnoreCase);
    public override Uri CreateAuthorizationUri(SocialAuthorizationRequest request) => SocialProviderHelpers.AuthorizationUri("https://x.com/i/oauth2/authorize", new Dictionary<string, string?>
    {
        ["client_id"] = configuration["X_CLIENT_ID"], ["redirect_uri"] = request.RedirectUri, ["response_type"] = "code",
        ["scope"] = "users.read users.email follows.read like.read offline.access", ["state"] = request.State,
        ["code_challenge"] = request.PkceChallenge, ["code_challenge_method"] = "S256"
    });
    public override async Task<SocialIdentityProfile> ExchangeAsync(SocialTokenExchangeRequest request, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "https://api.x.com/2/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["code"] = request.Code, ["grant_type"] = "authorization_code", ["client_id"] = configuration["X_CLIENT_ID"]!, ["redirect_uri"] = request.RedirectUri, ["code_verifier"] = request.PkceVerifier })
        };
        var basic = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{configuration["X_CLIENT_ID"]}:{configuration["X_CLIENT_SECRET"]}"));
        message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basic);
        using var response = await client.SendAsync(message, cancellationToken); var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"X token exchange failed with HTTP {(int)response.StatusCode}.");
        using var tokenJson = JsonDocument.Parse(body); var root = tokenJson.RootElement;
        var accessToken = SocialProviderHelpers.String(root, "access_token") ?? throw new InvalidOperationException("X did not return an access token.");
        using var profileRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/me?user.fields=name,username,profile_image_url");
        profileRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        using var profileResponse = await client.SendAsync(profileRequest, cancellationToken); profileResponse.EnsureSuccessStatusCode();
        using var profile = JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync(cancellationToken)); var data = profile.RootElement.GetProperty("data");
        return new SocialIdentityProfile(SocialProviderHelpers.String(data, "id")!, null, false, SocialProviderHelpers.String(data, "name"), SocialProviderHelpers.String(data, "username"),
            accessToken, SocialProviderHelpers.String(root, "refresh_token"), DateTimeOffset.UtcNow.AddSeconds(SocialProviderHelpers.Int64(root, "expires_in") ?? 7200),
            (SocialProviderHelpers.String(root, "scope") ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
    public override async Task<SocialActionVerificationResult> VerifyActionAsync(SocialActionVerificationRequest request, CancellationToken cancellationToken)
    {
        if (!SupportedActions.Contains(request.ActionType)) return await base.VerifyActionAsync(request, cancellationToken);
        var endpoint = request.ActionType.Equals("Follow", StringComparison.OrdinalIgnoreCase)
            ? $"https://api.x.com/2/users/{Uri.EscapeDataString(request.Subject)}/following?max_results=1000"
            : $"https://api.x.com/2/users/{Uri.EscapeDataString(request.Subject)}/liked_tweets?max_results=100";
        string? paginationToken = null;
        for (var page = 0; page < 10; page++)
        {
            var pageUri = paginationToken is null ? endpoint : QueryHelpers.AddQueryString(endpoint, "pagination_token", paginationToken);
            using var message = new HttpRequestMessage(HttpMethod.Get, pageUri); message.Headers.Authorization = new("Bearer", request.AccessToken);
            using var response = await client.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode) return new SocialActionVerificationResult("Failed", JsonSerializer.Serialize(new { statusCode = (int)response.StatusCode }));
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var matched = json.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array
                && data.EnumerateArray().Any(x => SocialProviderHelpers.String(x, "id") == request.TargetReference);
            if (matched) return new SocialActionVerificationResult("Verified", JsonSerializer.Serialize(new { provider = "X", request.ActionType, request.TargetReference }));
            paginationToken = json.RootElement.TryGetProperty("meta", out var meta) ? SocialProviderHelpers.String(meta, "next_token") : null;
            if (paginationToken is null) break;
        }
        return new SocialActionVerificationResult("NotVerified", JsonSerializer.Serialize(new { provider = "X", request.ActionType, request.TargetReference }));
    }
}

public sealed class TikTokSocialAuthProvider(HttpClient client, IConfiguration configuration) : UnsupportedActionSocialProvider
{
    public override string Name => "TikTok";
    public override string DisplayName => "TikTok";
    public override bool IsConfigured => !string.IsNullOrWhiteSpace(configuration["TIKTOK_CLIENT_KEY"]) && !string.IsNullOrWhiteSpace(configuration["TIKTOK_CLIENT_SECRET"]);
    public override Uri CreateAuthorizationUri(SocialAuthorizationRequest request) => SocialProviderHelpers.AuthorizationUri("https://www.tiktok.com/v2/auth/authorize/", new Dictionary<string, string?>
    {
        ["client_key"] = configuration["TIKTOK_CLIENT_KEY"], ["redirect_uri"] = request.RedirectUri, ["response_type"] = "code",
        ["scope"] = "user.info.basic", ["state"] = request.State
    });
    public override async Task<SocialIdentityProfile> ExchangeAsync(SocialTokenExchangeRequest request, CancellationToken cancellationToken)
    {
        using var tokenJson = await SocialProviderHelpers.PostFormAsync(client, "https://open.tiktokapis.com/v2/oauth/token/", new()
        {
            ["client_key"] = configuration["TIKTOK_CLIENT_KEY"]!, ["client_secret"] = configuration["TIKTOK_CLIENT_SECRET"]!,
            ["code"] = request.Code, ["grant_type"] = "authorization_code", ["redirect_uri"] = request.RedirectUri
        }, cancellationToken);
        var root = tokenJson.RootElement; var accessToken = SocialProviderHelpers.String(root, "access_token") ?? throw new InvalidOperationException("TikTok did not return an access token.");
        using var profileRequest = new HttpRequestMessage(HttpMethod.Get, "https://open.tiktokapis.com/v2/user/info/?fields=open_id,union_id,avatar_url,display_name");
        profileRequest.Headers.Authorization = new("Bearer", accessToken);
        using var profileResponse = await client.SendAsync(profileRequest, cancellationToken); profileResponse.EnsureSuccessStatusCode();
        using var profile = JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync(cancellationToken)); var data = profile.RootElement.GetProperty("data").GetProperty("user");
        return new SocialIdentityProfile(SocialProviderHelpers.String(data, "open_id")!, null, false, SocialProviderHelpers.String(data, "display_name"), null,
            accessToken, SocialProviderHelpers.String(root, "refresh_token"), DateTimeOffset.UtcNow.AddSeconds(SocialProviderHelpers.Int64(root, "expires_in") ?? 86400),
            (SocialProviderHelpers.String(root, "scope") ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries));
    }
}
