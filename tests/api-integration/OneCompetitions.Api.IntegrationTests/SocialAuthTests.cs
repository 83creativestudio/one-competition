using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneCompetitions.Contracts.Auth;
using OneCompetitions.Contracts.Competitions;
using OneCompetitions.Contracts.Entries;
using OneCompetitions.Contracts.SocialAuth;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Api.IntegrationTests;

public sealed class SocialAuthTests(TestApplicationFactory factory) : IClassFixture<TestApplicationFactory>
{
    [Theory]
    [InlineData("Google")]
    [InlineData("Apple")]
    [InlineData("Facebook")]
    [InlineData("X")]
    [InlineData("TikTok")]
    public async Task All_supported_providers_start_through_the_central_gateway(string provider)
    {
        await factory.SeedAsync();
        var owner = await OwnerAsync();
        var competition = await CreateAndPublishAsync(owner, [provider]);
        var client = PublicClient("one-digital.competitions.local");

        var response = await client.GetAsync($"/api/public/competitions/{competition.Slug}/social-auth/{provider}/start?returnUrl={Uri.EscapeDataString($"https://one-digital.competitions.local/{competition.Slug}/enter")}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("provider.test", response.Headers.Location!.Host);
        Assert.False(string.IsNullOrWhiteSpace(QueryHelpers.ParseQuery(response.Headers.Location.Query)["state"]));
        Assert.Equal($"http://auth.competitions.local/api/participant-auth/{provider.ToLowerInvariant()}/callback",
            QueryHelpers.ParseQuery(response.Headers.Location.Query)["redirect_uri"].ToString());

        var rejected = await client.GetAsync($"/api/public/competitions/{competition.Slug}/social-auth/{provider}/start?returnUrl={Uri.EscapeDataString("https://attacker.example/callback")}");
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
    }

    [Fact]
    public async Task Google_callback_creates_one_time_session_and_social_only_entry()
    {
        await factory.SeedAsync();
        var owner = await OwnerAsync();
        var competition = await CreateAndPublishAsync(owner, ["Google"]);
        var client = PublicClient("one-digital.competitions.local");
        var returnUrl = $"https://one-digital.competitions.local/{competition.Slug}/enter";

        var start = await client.GetAsync($"/api/public/competitions/{competition.Slug}/social-auth/Google/start?returnUrl={Uri.EscapeDataString(returnUrl)}");
        var state = QueryHelpers.ParseQuery(start.Headers.Location!.Query)["state"].ToString();
        var callbackClient = PublicClient("auth.competitions.local");
        var callback = await callbackClient.GetAsync($"/api/participant-auth/google/callback?code=google-user&state={Uri.EscapeDataString(state)}");
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        var completionCode = QueryHelpers.ParseQuery(callback.Headers.Location!.Query)["social_code"].ToString();

        var wrongTenant = PublicClient("omega-tv.competitions.local");
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await wrongTenant.PostAsJsonAsync("/api/public/social-auth/complete", new CompleteSocialAuthRequest(completionCode))).StatusCode);

        var completionResponse = await client.PostAsJsonAsync("/api/public/social-auth/complete", new CompleteSocialAuthRequest(completionCode));
        completionResponse.EnsureSuccessStatusCode();
        var session = await completionResponse.Content.ReadFromJsonAsync<SocialAuthSessionResponse>() ?? throw new InvalidOperationException();
        Assert.Equal("Google", session.Provider);
        Assert.Equal("google-user@social.test", session.Email);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/public/social-auth/complete", new CompleteSocialAuthRequest(completionCode))).StatusCode);

        client.DefaultRequestHeaders.Add("X-One-Participant-Session", session.ParticipantSessionToken);
        var entryResponse = await client.PostAsJsonAsync($"/api/public/competitions/{competition.Slug}/entries", EntryRequest("browser-supplied@example.test"));
        entryResponse.EnsureSuccessStatusCode();
        var entry = await entryResponse.Content.ReadFromJsonAsync<EntryResponse>() ?? throw new InvalidOperationException();
        Assert.Equal("Approved", entry.Status);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.CompetitionEntries.IgnoreQueryFilters().SingleAsync(x => x.Id == entry.Id);
        var participant = await db.Participants.IgnoreQueryFilters().SingleAsync(x => x.Id == stored.ParticipantId);
        Assert.Equal("Google", stored.EntryMethod);
        Assert.NotNull(stored.ParticipantIdentityId);
        Assert.Equal("google-user@social.test", participant.PrimaryEmail);
    }

    [Fact]
    public async Task Required_X_action_must_be_verified_before_entry()
    {
        await factory.SeedAsync();
        var owner = await OwnerAsync();
        var competition = await CreateAsync(owner, ["X"]);
        var actionResponse = await owner.PostAsJsonAsync($"/api/competitions/{competition.Id}/social-actions",
            new UpsertSocialActionRequirementRequest("X", "Follow", "123456", "Follow the organiser", true));
        actionResponse.EnsureSuccessStatusCode();
        var action = await actionResponse.Content.ReadFromJsonAsync<SocialActionRequirementResponse>() ?? throw new InvalidOperationException();
        (await owner.PostAsync($"/api/competitions/{competition.Id}/publish", null)).EnsureSuccessStatusCode();

        var client = PublicClient("one-digital.competitions.local");
        var start = await client.GetAsync($"/api/public/competitions/{competition.Slug}/social-auth/X/start?returnUrl={Uri.EscapeDataString($"https://one-digital.competitions.local/{competition.Slug}/enter")}");
        var state = QueryHelpers.ParseQuery(start.Headers.Location!.Query)["state"].ToString();
        var callback = await PublicClient("auth.competitions.local").GetAsync($"/api/participant-auth/x/callback?code=x-user&state={Uri.EscapeDataString(state)}");
        var completionCode = QueryHelpers.ParseQuery(callback.Headers.Location!.Query)["social_code"].ToString();
        var session = await (await client.PostAsJsonAsync("/api/public/social-auth/complete", new CompleteSocialAuthRequest(completionCode)))
            .Content.ReadFromJsonAsync<SocialAuthSessionResponse>() ?? throw new InvalidOperationException();
        client.DefaultRequestHeaders.Add("X-One-Participant-Session", session.ParticipantSessionToken);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/public/competitions/{competition.Slug}/entries", EntryRequest(null))).StatusCode);
        var verification = await client.PostAsync($"/api/public/social-actions/{action.Id}/verify", null);
        verification.EnsureSuccessStatusCode();
        Assert.Equal("Verified", (await verification.Content.ReadFromJsonAsync<SocialActionVerificationResponse>())!.Status);
        (await client.PostAsJsonAsync($"/api/public/competitions/{competition.Slug}/entries", EntryRequest(null))).EnsureSuccessStatusCode();
    }

    private async Task<CompetitionResponse> CreateAndPublishAsync(HttpClient owner, IReadOnlyList<string> providers)
    {
        var competition = await CreateAsync(owner, providers);
        (await owner.PostAsync($"/api/competitions/{competition.Id}/publish", null)).EnsureSuccessStatusCode();
        return competition;
    }

    private static async Task<CompetitionResponse> CreateAsync(HttpClient owner, IReadOnlyList<string> providers)
    {
        var slug = $"social-{Guid.NewGuid():N}";
        var response = await owner.PostAsJsonAsync("/api/competitions", new CreateCompetitionRequest("Social competition", slug, null,
            DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1), null, 1, 1, 0, false,
            AllowedParticipantAuthProviders: providers));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CompetitionResponse>() ?? throw new InvalidOperationException();
    }

    private async Task<HttpClient> OwnerAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("owner@one.local", "DevelopmentOnly!ChangeMe123"));
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>() ?? throw new InvalidOperationException();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        client.DefaultRequestHeaders.Host = "one-digital.competitions.local";
        return client;
    }

    private HttpClient PublicClient(string host)
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Host = host;
        return client;
    }

    private static SubmitEntryRequest EntryRequest(string? email) => new(email, null, "Social", "Participant", "en",
        Guid.NewGuid().ToString("N"), null, [], [], CaptchaToken: "test-pass");
}
