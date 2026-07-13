using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using OneCompetitions.Contracts.Auth;
using OneCompetitions.Contracts.Tenants;

namespace OneCompetitions.Api.IntegrationTests;

public sealed class AuthAndTenantTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public AuthAndTenantTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task User_can_log_in_and_read_their_current_tenant()
    {
        await _factory.SeedAsync();
        var client = _factory.CreateClient();

        var auth = await LoginAsync(client, "owner@one.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        client.DefaultRequestHeaders.Host = "one-digital.competitions.local";

        var tenantResponse = await client.GetAsync("/api/tenants/current");
        await EnsureSuccessAsync(tenantResponse);
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResponse>();

        Assert.NotNull(tenant);
        Assert.Equal("one-digital", tenant!.Slug);
    }

    [Fact]
    public async Task Platform_administrator_can_list_all_tenants()
    {
        await _factory.SeedAsync();
        var client = _factory.CreateClient();

        var auth = await LoginAsync(client, "admin@onecompetitions.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var tenantsResponse = await client.GetAsync("/api/platform/tenants");
        await EnsureSuccessAsync(tenantsResponse);
        var tenants = await tenantsResponse.Content.ReadFromJsonAsync<List<TenantSummaryResponse>>();

        Assert.NotNull(tenants);
        Assert.Contains(tenants!, x => x.Slug == "one-digital");
        Assert.Contains(tenants!, x => x.Slug == "omega-tv");
    }

    [Fact]
    public async Task Tenant_user_cannot_access_another_tenant_context()
    {
        await _factory.SeedAsync();
        var client = _factory.CreateClient();

        var auth = await LoginAsync(client, "owner@one.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        client.DefaultRequestHeaders.Host = "omega-tv.competitions.local";

        var response = await client.GetAsync("/api/tenants/current");

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, await DescribeAsync(response));
    }

    private static async Task<AuthResponse> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "DevelopmentOnly!ChangeMe123"));
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed with {(int)response.StatusCode}: {body}");
        }

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return auth ?? throw new InvalidOperationException("Missing auth response.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await DescribeAsync(response));
        }
    }

    private static async Task<string> DescribeAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var authError = response.Headers.TryGetValues("X-Auth-Error", out var values) ? string.Join(" | ", values) : string.Empty;
        return $"Expected success/forbidden but received {(int)response.StatusCode}. AuthError={authError}. Body={body}";
    }
}
