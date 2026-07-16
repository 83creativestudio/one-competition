using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneCompetitions.Application.Jobs;
using OneCompetitions.Contracts.Assets;
using OneCompetitions.Contracts.Auth;
using OneCompetitions.Contracts.Competitions;
using OneCompetitions.Contracts.Webhooks;
using OneCompetitions.Domain.Competitions;
using OneCompetitions.Domain.Tenants;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Api.IntegrationTests;

public sealed class ProductionOperationsTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public ProductionOperationsTests(TestApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Tenant_can_upload_scan_download_asset_and_manage_encrypted_webhook()
    {
        await _factory.SeedAsync();
        var owner = await CreateClientAsync("owner@one.local");
        owner.DefaultRequestHeaders.Host = "one-digital.competitions.local";
        var pdf = "%PDF-1.4\n%%EOF"u8.ToArray();

        var uploadResponse = await owner.PostAsJsonAsync("/api/assets/uploads", new CreateAssetUploadRequest(null, null, "Rules", "rules.pdf", "application/pdf", pdf.Length));
        uploadResponse.EnsureSuccessStatusCode();
        var upload = await uploadResponse.Content.ReadFromJsonAsync<AssetUploadResponse>() ?? throw new InvalidOperationException("Missing upload response.");
        using var content = new ByteArrayContent(pdf);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        var put = await owner.PutAsync(upload.UploadUrl, content);
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var complete = await owner.PostAsync($"/api/assets/{upload.AssetId}/complete", null);
        complete.EnsureSuccessStatusCode();
        var asset = await complete.Content.ReadFromJsonAsync<AssetResponse>() ?? throw new InvalidOperationException("Missing asset.");
        Assert.Equal("Clean", asset.ScanStatus);
        Assert.Equal(64, asset.Sha256Hash.Length);
        var downloadRedirect = await owner.GetAsync($"/api/assets/{asset.Id}/download");
        Assert.Equal(HttpStatusCode.Redirect, downloadRedirect.StatusCode);
        var download = await owner.GetAsync(downloadRedirect.Headers.Location);
        download.EnsureSuccessStatusCode();
        Assert.Equal(pdf, await download.Content.ReadAsByteArrayAsync());

        var webhookResponse = await owner.PostAsJsonAsync("/api/webhooks", new CreateWebhookEndpointRequest("https://hooks.example.com/competitions", ["entry.submitted"]));
        webhookResponse.EnsureSuccessStatusCode();
        var created = await webhookResponse.Content.ReadFromJsonAsync<WebhookEndpointCreatedResponse>() ?? throw new InvalidOperationException("Missing webhook.");
        Assert.StartsWith("whsec_", created.SigningSecret);
        var listed = await owner.GetFromJsonAsync<List<WebhookEndpointResponse>>("/api/webhooks");
        Assert.Contains(listed!, x => x.Id == created.Endpoint.Id && x.IsActive);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.WebhookEndpoints.IgnoreQueryFilters().SingleAsync(x => x.Id == created.Endpoint.Id);
        Assert.DoesNotContain(created.SigningSecret, stored.SecretCiphertext);
        Assert.DoesNotContain(created.SigningSecret, stored.SecretHash);
    }

    [Fact]
    public async Task Worker_closes_expired_competition_once_and_audits_the_transition()
    {
        await _factory.SeedAsync();
        var owner = await CreateClientAsync("owner@one.local");
        owner.DefaultRequestHeaders.Host = "one-digital.competitions.local";
        var response = await owner.PostAsJsonAsync("/api/competitions", new CreateCompetitionRequest(
            "Expired worker draw", $"expired-worker-{Guid.NewGuid():N}", null, DateTimeOffset.UtcNow.AddDays(-2),
            DateTimeOffset.UtcNow.AddDays(1), null, 1, 1, 0, false));
        response.EnsureSuccessStatusCode();
        var competition = await response.Content.ReadFromJsonAsync<CompetitionResponse>() ?? throw new InvalidOperationException("Missing competition.");

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.Competitions.IgnoreQueryFilters().SingleAsync(x => x.Id == competition.Id);
            entity.Status = CompetitionStatus.Live;
            entity.EndsAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        await RunWorkerAsync();
        await RunWorkerAsync();
        var closed = await owner.GetFromJsonAsync<CompetitionResponse>($"/api/competitions/{competition.Id}");
        Assert.Equal("Closed", closed!.Status);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await verifyDb.AuditEvents.IgnoreQueryFilters().CountAsync(x => x.EntityId == competition.Id.ToString() && x.Action == "competition.closed.automatic"));
    }

    [Fact]
    public async Task Suspended_subscription_cannot_create_plan_controlled_resources()
    {
        await _factory.SeedAsync();
        var owner = await CreateClientAsync("owner@one.local");
        owner.DefaultRequestHeaders.Host = "one-digital.competitions.local";
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tenant = await db.Tenants.IgnoreQueryFilters().SingleAsync(x => x.Slug == "one-digital");
            tenant.Status = TenantStatus.Suspended;
            tenant.SuspendedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        try
        {
            var response = await owner.PostAsJsonAsync("/api/competitions", new CreateCompetitionRequest(
                "Blocked by subscription", $"blocked-{Guid.NewGuid():N}", null, DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(1), null, 1, 1, 0, false));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tenant = await db.Tenants.IgnoreQueryFilters().SingleAsync(x => x.Slug == "one-digital");
            tenant.Status = TenantStatus.Active;
            tenant.SuspendedAt = null;
            await db.SaveChangesAsync();
        }
    }

    private async Task RunWorkerAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IPlatformJobProcessor>().RunOnceAsync(CancellationToken.None);
    }

    private async Task<HttpClient> CreateClientAsync(string email)
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "DevelopmentOnly!ChangeMe123"));
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>() ?? throw new InvalidOperationException("Missing auth.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }
}
