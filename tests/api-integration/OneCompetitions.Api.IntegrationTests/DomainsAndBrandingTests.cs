using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneCompetitions.Contracts.Auth;
using OneCompetitions.Contracts.Branding;
using OneCompetitions.Contracts.Domains;
using OneCompetitions.Contracts.Tenants;
using OneCompetitions.Domain.Tenants;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Api.IntegrationTests;

public sealed class DomainsAndBrandingTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public DomainsAndBrandingTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Tenant_owner_can_create_and_verify_development_custom_domain()
    {
        await _factory.SeedAsync();
        var client = await CreateTenantOwnerClientAsync();
        client.DefaultRequestHeaders.Host = "one-digital.competitions.local";

        var createResponse = await client.PostAsJsonAsync("/api/domains", new CreateTenantDomainRequest("win.verified.test", "CustomSubdomain"));
        createResponse.EnsureSuccessStatusCode();
        var domain = await createResponse.Content.ReadFromJsonAsync<TenantDomainResponse>() ?? throw new InvalidOperationException("Missing domain response.");

        Assert.Equal("AwaitingDns", domain.Status);
        Assert.StartsWith("one-competitions-verify=", domain.ExpectedDnsTarget);

        var verifyResponse = await client.PostAsync($"/api/domains/{domain.Id}/verify", null);
        verifyResponse.EnsureSuccessStatusCode();
        var verification = await verifyResponse.Content.ReadFromJsonAsync<DomainVerificationResponse>() ?? throw new InvalidOperationException("Missing verification response.");

        Assert.True(verification.IsVerified);
        Assert.Equal("Active", verification.Status);

        var publicClient = _factory.CreateClient();
        publicClient.DefaultRequestHeaders.Host = "win.verified.test";
        var theme = await publicClient.GetFromJsonAsync<PublicThemeResponse>("/api/public/theme");

        Assert.NotNull(theme);
        Assert.Equal("one-digital", theme!.TenantSlug);
    }

    [Fact]
    public async Task Tenant_owner_can_create_brand_and_public_theme_resolves_from_platform_subdomain()
    {
        await _factory.SeedAsync();
        var client = await CreateTenantOwnerClientAsync();
        client.DefaultRequestHeaders.Host = "one-digital.competitions.local";

        var request = new UpsertBrandProfileRequest(
            "Campaign Brand",
            "#14532d",
            "#111827",
            "#ea580c",
            "#ffffff",
            "#111827",
            "Inter",
            "Inter",
            "Solid",
            8,
            "Campaign footer",
            "support@example.test",
            null,
            false,
            ".entry-button { font-weight: 700; }");

        var createResponse = await client.PostAsJsonAsync("/api/brands", request);
        createResponse.EnsureSuccessStatusCode();

        var publicClient = _factory.CreateClient();
        publicClient.DefaultRequestHeaders.Host = "one-digital.competitions.local";
        var theme = await publicClient.GetFromJsonAsync<PublicThemeResponse>("/api/public/theme");

        Assert.NotNull(theme);
        Assert.Equal("one-digital", theme!.TenantSlug);
        Assert.Equal("ONE. Digital", theme.BrandName);
    }

    [Fact]
    public async Task Public_theme_resolves_from_platform_path_route()
    {
        await _factory.SeedAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Host = "competitions.local";

        var theme = await client.GetFromJsonAsync<PublicThemeResponse>("/c/one-digital/api/public/theme");

        Assert.NotNull(theme);
        Assert.Equal("one-digital", theme!.TenantSlug);
        Assert.Equal("ONE. Digital", theme.BrandName);
    }

    [Theory]
    [InlineData("competitions.local")]
    [InlineData("auth.competitions.local")]
    [InlineData("other-tenant.competitions.local")]
    public async Task Platform_owned_names_cannot_be_registered_as_customer_domains(string hostname)
    {
        await _factory.SeedAsync();
        var client = await CreateTenantOwnerClientAsync();
        client.DefaultRequestHeaders.Host = "one-digital.competitions.local";
        var response = await client.PostAsJsonAsync("/api/domains", new CreateTenantDomainRequest(hostname, "CustomSubdomain"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("reserved", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Failed_legacy_customer_binding_cannot_disable_or_claim_the_shared_platform_host()
    {
        await _factory.SeedAsync();
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tenant = await db.Tenants.SingleAsync(x => x.Slug == "one-digital");
            db.TenantDomains.Add(new TenantDomain { Id = Guid.NewGuid(), TenantId = tenant.Id,
                Hostname = "competitions.local", DomainType = TenantDomainType.CustomSubdomain,
                Status = TenantDomainStatus.DnsError, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
        var anonymous = _factory.CreateClient();
        anonymous.DefaultRequestHeaders.Host = "competitions.local";
        var theme = await anonymous.GetFromJsonAsync<PublicThemeResponse>("/c/omega-tv/api/public/theme");
        Assert.Equal("omega-tv", theme!.TenantSlug);
        var admin = _factory.CreateClient();
        admin.DefaultRequestHeaders.Host = "competitions.local";
        var login = await admin.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@onecompetitions.local", "DevelopmentOnly!ChangeMe123"));
        login.EnsureSuccessStatusCode();
        var auth = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        admin.DefaultRequestHeaders.Add("X-One-Tenant", "omega-tv");
        var selected = await admin.GetFromJsonAsync<TenantResponse>("/api/tenants/current");
        Assert.Equal("omega-tv", selected!.Slug);
    }

    [Fact]
    public async Task Inactive_real_custom_domain_still_rejects_authenticated_requests()
    {
        await _factory.SeedAsync();
        var hostname = $"suspended-{Guid.NewGuid():N}.test";
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tenant = await db.Tenants.SingleAsync(x => x.Slug == "one-digital");
            db.TenantDomains.Add(new TenantDomain { Id = Guid.NewGuid(), TenantId = tenant.Id,
                Hostname = hostname, DomainType = TenantDomainType.CustomSubdomain,
                Status = TenantDomainStatus.Suspended, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
        var client = await CreateTenantOwnerClientAsync();
        client.DefaultRequestHeaders.Host = hostname;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/tenants/current")).StatusCode);
    }

    private async Task<HttpClient> CreateTenantOwnerClientAsync()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("owner@one.local", "DevelopmentOnly!ChangeMe123"));
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>() ?? throw new InvalidOperationException("Missing auth response.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }
}
