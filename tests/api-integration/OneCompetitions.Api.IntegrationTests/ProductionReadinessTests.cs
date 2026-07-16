using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneCompetitions.Contracts.Auth;
using OneCompetitions.Contracts.Competitions;
using OneCompetitions.Contracts.Entries;
using OneCompetitions.Contracts.Platform;
using OneCompetitions.Contracts.Privacy;
using OneCompetitions.Contracts.Tenants;
using OneCompetitions.Domain.Entries;
using OneCompetitions.Domain.Notifications;
using OneCompetitions.Domain.Participants;
using OneCompetitions.Infrastructure.Persistence;
using OneCompetitions.Infrastructure.Services;

namespace OneCompetitions.Api.IntegrationTests;

public sealed class ProductionReadinessTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;
    public ProductionReadinessTests(TestApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Invitation_acceptance_and_mfa_challenge_are_enforced()
    {
        await _factory.SeedAsync();
        var owner = await AuthenticatedAsync("owner@one.local"); owner.DefaultRequestHeaders.Host = "one-digital.competitions.local";
        var email = $"invited-{Guid.NewGuid():N}@example.com";
        var inviteResponse = await owner.PostAsJsonAsync("/api/tenants/current/users/invite", new InviteTenantUserRequest(email, "Viewer"));
        inviteResponse.EnsureSuccessStatusCode();
        var invitation = await inviteResponse.Content.ReadFromJsonAsync<TenantUserResponse>() ?? throw new InvalidOperationException();
        var message = await NotificationAsync(email, "invited");
        var token = LineValue(message.BodyText, "One-time token:");

        var anonymous = _factory.CreateClient();
        var accepted = await anonymous.PostAsJsonAsync("/api/auth/invitations/accept", new AcceptInvitationRequest(invitation.Id, token, "DevelopmentOnly!ChangeMe456", "Invited Reviewer"));
        Assert.True(accepted.StatusCode == HttpStatusCode.NoContent, await accepted.Content.ReadAsStringAsync());

        var setupResponse = await anonymous.PostAsJsonAsync("/api/auth/mfa/setup", new MfaSetupRequest(email, "DevelopmentOnly!ChangeMe456"));
        setupResponse.EnsureSuccessStatusCode();
        var setup = await setupResponse.Content.ReadFromJsonAsync<MfaSetupResponse>() ?? throw new InvalidOperationException();
        var code = Totp(setup.SharedKey);
        var enabled = await anonymous.PostAsJsonAsync("/api/auth/mfa/enable", new MfaEnableRequest(email, "DevelopmentOnly!ChangeMe456", code));
        Assert.Equal(HttpStatusCode.NoContent, enabled.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "DevelopmentOnly!ChangeMe456"))).StatusCode);
        var login = await anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "DevelopmentOnly!ChangeMe456", Totp(setup.SharedKey)));
        login.EnsureSuccessStatusCode();

        var session = await login.Content.ReadFromJsonAsync<AuthResponse>() ?? throw new InvalidOperationException();
        (await anonymous.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest(email))).EnsureSuccessStatusCode();
        var resetMessage = await NotificationAsync(email, "Reset your ONE. Competitions password");
        var resetToken = resetMessage.BodyText.Split("\n\n", StringSplitOptions.RemoveEmptyEntries).Last().Trim();
        var reset = await anonymous.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(email, resetToken, "DevelopmentOnly!Changed789"));
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(session.RefreshToken))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "DevelopmentOnly!ChangeMe456", Totp(setup.SharedKey)))).StatusCode);
        (await anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "DevelopmentOnly!Changed789", Totp(setup.SharedKey)))).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Captcha_eligibility_verification_and_sensitive_answer_encryption_work_together()
    {
        await _factory.SeedAsync();
        var owner = await AuthenticatedAsync("owner@one.local"); owner.DefaultRequestHeaders.Host = "one-digital.competitions.local";
        var slug = $"secure-entry-{Guid.NewGuid():N}";
        var created = await owner.PostAsJsonAsync("/api/competitions", new CreateCompetitionRequest("Secure entry", slug, null,
            DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1), null, 2, 1, 0, false, 18, ["CY"], true, false));
        created.EnsureSuccessStatusCode();
        var competition = await created.Content.ReadFromJsonAsync<CompetitionResponse>() ?? throw new InvalidOperationException();
        var field = await owner.PostAsJsonAsync($"/api/competitions/{competition.Id}/fields", new UpsertCompetitionFieldRequest(
            "passport", "Text", "Passport", null, null, true, 1, "{}", "{}", true, false, false));
        field.EnsureSuccessStatusCode();
        (await owner.PostAsync($"/api/competitions/{competition.Id}/publish", null)).EnsureSuccessStatusCode();

        var publicClient = _factory.CreateClient(); publicClient.DefaultRequestHeaders.Host = "one-digital.competitions.local";
        var request = new SubmitEntryRequest("secure@example.com", null, "Secure", "Participant", "en", Guid.NewGuid().ToString("N"), null,
            [new EntryAnswerRequest("passport", "SECRET-123")], [], "invalid", "device-one", DateTimeOffset.UtcNow.AddSeconds(-10), new DateOnly(1990, 1, 1), "CY");
        Assert.Equal(HttpStatusCode.Unauthorized, (await publicClient.PostAsJsonAsync($"/api/public/competitions/{slug}/entries", request)).StatusCode);
        var valid = request with { CaptchaToken = "test-pass" };
        var submittedResponse = await publicClient.PostAsJsonAsync($"/api/public/competitions/{slug}/entries", valid);
        Assert.True(submittedResponse.IsSuccessStatusCode, await submittedResponse.Content.ReadAsStringAsync());
        var submitted = await submittedResponse.Content.ReadFromJsonAsync<EntryResponse>() ?? throw new InvalidOperationException();
        Assert.Equal("EmailVerificationPending", submitted.Status);
        var verification = await NotificationAsync("secure@example.com", "Verify your competition entry");
        var code = Regex.Match(verification.BodyText, @"\b\d{6}\b").Value;
        var verifiedResponse = await publicClient.PostAsJsonAsync($"/api/public/entries/{submitted.EntryReference}/verify-email", new VerifyEntryRequest(code));
        verifiedResponse.EnsureSuccessStatusCode();
        Assert.Equal("Approved", (await verifiedResponse.Content.ReadFromJsonAsync<EntryResponse>())!.Status);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var answer = await db.EntryAnswers.SingleAsync(x => x.EntryId == submitted.Id);
        Assert.Null(answer.StringValue); Assert.NotEqual("SECRET-123", answer.EncryptedValue);
        Assert.Equal("SECRET-123", scope.ServiceProvider.GetRequiredService<SecretProtector>().Unprotect(answer.EncryptedValue!));
    }

    [Fact]
    public async Task Privacy_export_is_email_authorized_and_tenant_scoped()
    {
        await _factory.SeedAsync();
        var email = $"privacy-{Guid.NewGuid():N}@example.com";
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tenant = await db.Tenants.IgnoreQueryFilters().SingleAsync(x => x.Slug == "one-digital");
            db.Participants.Add(new Participant { Id = Guid.NewGuid(), TenantId = tenant.Id, PrimaryEmail = email, FirstName = "Privacy", LastName = "User", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
        var client = _factory.CreateClient(); client.DefaultRequestHeaders.Host = "one-digital.competitions.local";
        var request = await client.PostAsJsonAsync("/api/public/privacy/requests", new CreatePrivacyRequest(email, "Export"));
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        var notification = await NotificationAsync(email, "Confirm your privacy request");
        var reference = LineValue(notification.BodyText, "Privacy request:"); var token = LineValue(notification.BodyText, "One-time token:");
        var complete = await client.PostAsJsonAsync($"/api/public/privacy/requests/{reference}/complete", new CompletePrivacyRequest(token));
        complete.EnsureSuccessStatusCode();
        var result = await complete.Content.ReadFromJsonAsync<PrivacyRequestResult>();
        Assert.Equal("Completed", result!.Status); Assert.NotNull(result.DownloadUrl);
    }

    [Fact]
    public async Task Platform_administration_is_available_only_to_platform_admin()
    {
        await _factory.SeedAsync();
        var owner = await AuthenticatedAsync("owner@one.local");
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/platform/domains")).StatusCode);
        var admin = await AuthenticatedAsync("admin@onecompetitions.local");
        var tenants = await admin.GetFromJsonAsync<List<TenantSummaryResponse>>("/api/platform/tenants") ?? [];
        var detail = await admin.GetAsync($"/api/platform/tenants/{tenants[0].Id}");
        detail.EnsureSuccessStatusCode();
        Assert.NotNull(await detail.Content.ReadFromJsonAsync<PlatformTenantDetailResponse>());
    }

    [Fact]
    public async Task Turnstile_provider_sends_the_expected_verification_contract()
    {
        string? requestBody = null;
        var handler = new StubHttpHandler(async request =>
        {
            requestBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
            };
        });
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TURNSTILE_SECRET_KEY"] = "provider-secret"
        }).Build();
        var provider = new TurnstileCaptchaProvider(new HttpClient(handler), configuration);

        Assert.True(await provider.ValidateAsync("participant-token", "203.0.113.10", CancellationToken.None));
        Assert.Contains("secret=provider-secret", requestBody);
        Assert.Contains("response=participant-token", requestBody);
        Assert.Contains("remoteip=203.0.113.10", requestBody);
    }

    private async Task<HttpClient> AuthenticatedAsync(string email)
    {
        var client = _factory.CreateClient(); var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "DevelopmentOnly!ChangeMe123")); response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>() ?? throw new InvalidOperationException(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken); return client;
    }

    private async Task<NotificationMessage> NotificationAsync(string recipient, string subject)
    {
        await using var scope = _factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.NotificationMessages.IgnoreQueryFilters().Where(x => x.Recipient == recipient && x.Subject!.Contains(subject)).ToListAsync()).OrderByDescending(x => x.CreatedAt).First();
    }
    private static string LineValue(string body, string prefix) => body.Split('\n').Single(x => x.StartsWith(prefix, StringComparison.Ordinal))[prefix.Length..].Trim();

    private static string Totp(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"; var bits = new List<bool>();
        foreach (var c in base32.Trim().TrimEnd('=').ToUpperInvariant()) { var value = alphabet.IndexOf(c); for (var i = 4; i >= 0; i--) bits.Add((value & (1 << i)) != 0); }
        var key = new byte[bits.Count / 8]; for (var i = 0; i < key.Length; i++) for (var j = 0; j < 8; j++) if (bits[i * 8 + j]) key[i] |= (byte)(1 << (7 - j));
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30; var data = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(counter));
        var hash = HMACSHA1.HashData(key, data); var offset = hash[^1] & 0xf; var binary = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6");
    }

    private sealed class StubHttpHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
}
