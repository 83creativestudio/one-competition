using System.Net;
using System.Net.Http.Json;
using OneCompetitions.Contracts.Auth;

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
}
