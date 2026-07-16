using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneCompetitions.Contracts.Auth;
using OneCompetitions.Contracts.Billing;
using OneCompetitions.Domain.Competitions;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Security.Tests;

public sealed class SecurityBoundaryTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public SecurityBoundaryTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Unknown_custom_host_is_rejected_before_authorized_api_access()
    {
        await _factory.SeedAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Host = "unknown.customer.example";

        var response = await client.GetAsync("/api/tenants/current");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Tenant_user_cannot_access_platform_tenant_list()
    {
        await _factory.SeedAsync();
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("owner@one.local", "DevelopmentOnly!ChangeMe123"));
        if (!login.IsSuccessStatusCode)
        {
            var loginBody = await login.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed with {(int)login.StatusCode}: {loginBody}");
        }

        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>() ?? throw new InvalidOperationException();
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.AccessToken);

        var response = await client.GetAsync("/api/platform/tenants");

        var body = await response.Content.ReadAsStringAsync();
        var authError = response.Headers.TryGetValues("X-Auth-Error", out var values) ? string.Join(" | ", values) : string.Empty;
        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, $"Expected forbidden but received {(int)response.StatusCode}. AuthError={authError}. Body={body}");
    }

    [Fact]
    public async Task Tenant_user_cannot_read_another_tenants_competition_by_id()
    {
        await _factory.SeedAsync();
        var competitionId = Guid.NewGuid();
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var omegaTenant = await db.Tenants.IgnoreQueryFilters().SingleAsync(x => x.Slug == "omega-tv");
            db.Competitions.Add(new Competition
            {
                Id = competitionId, TenantId = omegaTenant.Id, Name = "Private Omega Draw", Slug = $"private-omega-{Guid.NewGuid():N}",
                Status = CompetitionStatus.Draft, StartsAt = DateTimeOffset.UtcNow, EndsAt = DateTimeOffset.UtcNow.AddDays(1),
                CreatedByUserId = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var oneDigital = await AuthenticatedAsync("owner@one.local");
        oneDigital.DefaultRequestHeaders.Host = "one-digital.competitions.local";
        Assert.Equal(HttpStatusCode.NotFound, (await oneDigital.GetAsync($"/api/competitions/{competitionId}")).StatusCode);
    }

    [Fact]
    public async Task Billing_redirects_must_remain_on_platform_https_hosts()
    {
        await _factory.SeedAsync();
        var client = await AuthenticatedAsync("owner@one.local");
        client.DefaultRequestHeaders.Host = "one-digital.competitions.local";
        var response = await client.PostAsJsonAsync("/api/billing/checkout", new CreateCheckoutSessionRequest(
            Guid.NewGuid(), "Monthly", "https://attacker.example/success", "https://attacker.example/cancel"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpClient> AuthenticatedAsync(string email)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "DevelopmentOnly!ChangeMe123"));
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>() ?? throw new InvalidOperationException();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }
}
