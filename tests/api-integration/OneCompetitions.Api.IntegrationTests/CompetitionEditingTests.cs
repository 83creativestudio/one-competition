using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneCompetitions.Contracts.Auth;
using OneCompetitions.Contracts.Competitions;
using OneCompetitions.Domain.Draws;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Api.IntegrationTests;

public sealed class CompetitionEditingTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;
    public CompetitionEditingTests(TestApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Global_disable_is_tenant_scoped_and_individual_competitions_can_opt_back_in()
    {
        await _factory.SeedAsync();
        var owner = await ClientAsync("owner@one.local", "one-digital");
        var item = await CreateAsync(owner);
        Assert.False(item.RequiresEmailVerification);
        (await owner.PatchAsJsonAsync($"/api/competitions/{item.Id}/email-verification", new UpdateEmailVerificationRequest(true))).EnsureSuccessStatusCode();
        var admin = await ClientAsync("admin@onecompetitions.local", "omega-tv");
        var foreignItem = await CreateAsync(admin);
        (await admin.PatchAsJsonAsync($"/api/competitions/{foreignItem.Id}/email-verification", new UpdateEmailVerificationRequest(true))).EnsureSuccessStatusCode();
        var response = await owner.PostAsync("/api/competitions/email-verification/disable-all", null);
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<EmailVerificationResetResponse>())!;
        Assert.True(result.UpdatedCompetitions >= 1);
        Assert.False((await owner.GetFromJsonAsync<CompetitionResponse>($"/api/competitions/{item.Id}"))!.RequiresEmailVerification);
        Assert.True((await admin.GetFromJsonAsync<CompetitionResponse>($"/api/competitions/{foreignItem.Id}"))!.RequiresEmailVerification);
        (await owner.PatchAsJsonAsync($"/api/competitions/{item.Id}/email-verification", new UpdateEmailVerificationRequest(true))).EnsureSuccessStatusCode();
        Assert.True((await owner.GetFromJsonAsync<CompetitionResponse>($"/api/competitions/{item.Id}"))!.RequiresEmailVerification);
        var viewer = await ClientAsync("owner@omega.local", "omega-tv");
        Assert.False((await viewer.PostAsync("/api/competitions/email-verification/disable-all", null)).IsSuccessStatusCode);
    }

    [Fact]
    public async Task Email_verification_can_be_toggled_without_changing_other_settings()
    {
        await _factory.SeedAsync();
        var owner = await ClientAsync("owner@one.local", "one-digital");
        var item = await CreateAsync(owner);
        (await owner.PostAsync($"/api/competitions/{item.Id}/publish", null)).EnsureSuccessStatusCode();
        foreach (var enabled in new[] { true, false })
        {
            var response = await owner.PatchAsJsonAsync($"/api/competitions/{item.Id}/email-verification", new UpdateEmailVerificationRequest(enabled));
            response.EnsureSuccessStatusCode();
            var updated = (await response.Content.ReadFromJsonAsync<CompetitionResponse>())!;
            Assert.Equal(enabled, updated.RequiresEmailVerification);
            Assert.Equal(item.Name, updated.Name);
            Assert.Equal(item.EndsAt, updated.EndsAt);
            Assert.Equal(item.NumberOfWinners, updated.NumberOfWinners);
        }
        var foreign = await ClientAsync("owner@omega.local", "omega-tv");
        Assert.False((await foreign.PatchAsJsonAsync($"/api/competitions/{item.Id}/email-verification", new UpdateEmailVerificationRequest(false))).IsSuccessStatusCode);
    }

    [Fact]
    public async Task Published_field_settings_are_editable_and_previous_wording_is_versioned()
    {
        await _factory.SeedAsync();
        var owner = await ClientAsync("owner@one.local", "one-digital");
        var item = await CreateAsync(owner);
        var original = new UpsertCompetitionFieldRequest("consent", "Consent", "Original terms", null, "Original help", true, 1, "{}", "[]", false, false, true);
        var created = await owner.PostAsJsonAsync($"/api/competitions/{item.Id}/fields", original);
        created.EnsureSuccessStatusCode();
        var field = (await created.Content.ReadFromJsonAsync<CompetitionFieldResponse>())!;
        (await owner.PostAsync($"/api/competitions/{item.Id}/publish", null)).EnsureSuccessStatusCode();
        var response = await owner.PatchAsJsonAsync($"/api/competitions/{item.Id}/fields/{field.Id}", original with { Label = "Updated terms", HelpText = "Updated help", IsRequired = false, Placeholder = "Updated placeholder" });
        response.EnsureSuccessStatusCode();
        var updated = (await response.Content.ReadFromJsonAsync<CompetitionFieldResponse>())!;
        Assert.Equal("Updated terms", updated.Label);
        Assert.Equal("Updated help", updated.HelpText);
        Assert.False(updated.IsRequired);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var version = await db.CompetitionVersions.OrderByDescending(x => x.VersionNumber).FirstAsync(x => x.CompetitionId == item.Id);
        using var snapshot = JsonDocument.Parse(version.ConfigurationJson);
        Assert.Equal("Original terms", snapshot.RootElement.GetProperty("Fields")[0].GetProperty("Label").GetString());
        var foreign = await ClientAsync("owner@omega.local", "omega-tv");
        Assert.False((await foreign.PatchAsJsonAsync($"/api/competitions/{item.Id}/fields/{field.Id}", original)).IsSuccessStatusCode);
    }

    [Fact]
    public async Task Owner_can_edit_a_draft_and_preserve_its_public_slug()
    {
        await _factory.SeedAsync();
        var owner = await ClientAsync("owner@one.local", "one-digital");
        var item = await CreateAsync(owner);
        var request = Update(item) with { Name = "Edited competition", Description = "Edited description", EntryLimit = 500,
            NumberOfReserveWinners = 3, MinimumAge = 18, AllowedCountryCodes = ["cy", "GR"], RequiresEmailVerification = true };
        var response = await owner.PatchAsJsonAsync($"/api/competitions/{item.Id}", request);
        response.EnsureSuccessStatusCode();
        var updated = (await owner.GetFromJsonAsync<CompetitionResponse>($"/api/competitions/{item.Id}"))!;
        Assert.Equal(request.Name, updated.Name);
        Assert.Equal(request.Description, updated.Description);
        Assert.Equal(item.Slug, updated.Slug);
        Assert.Equal(500, updated.EntryLimit);
        Assert.Equal(18, updated.MinimumAge);
        Assert.Equal(["CY", "GR"], updated.AllowedCountryCodes);
        Assert.True(updated.RequiresEmailVerification);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.AuditEvents.IgnoreQueryFilters().AnyAsync(x => x.EntityId == item.Id.ToString() && x.Action == "competition.updated"));
    }

    [Fact]
    public async Task Published_edit_preserves_the_previous_configuration_version()
    {
        await _factory.SeedAsync();
        var owner = await ClientAsync("owner@one.local", "one-digital");
        var item = await CreateAsync(owner);
        (await owner.PostAsync($"/api/competitions/{item.Id}/publish", null)).EnsureSuccessStatusCode();
        (await owner.PatchAsJsonAsync($"/api/competitions/{item.Id}", Update(item) with { Name = "Changed live name", Description = "New description" })).EnsureSuccessStatusCode();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var versions = await db.CompetitionVersions.IgnoreQueryFilters().Where(x => x.CompetitionId == item.Id).OrderBy(x => x.VersionNumber).ToListAsync();
        Assert.Equal(2, versions.Count);
        using var snapshot = JsonDocument.Parse(versions[^1].ConfigurationJson);
        Assert.Equal(item.Name, snapshot.RootElement.GetProperty("Name").GetString());
        Assert.Equal(item.Description, snapshot.RootElement.GetProperty("Description").GetString());
        Assert.Equal(item.PerParticipantEntryLimit, snapshot.RootElement.GetProperty("PerParticipantEntryLimit").GetInt32());
    }

    [Fact]
    public async Task Invalid_settings_are_rejected_without_changing_the_competition()
    {
        await _factory.SeedAsync();
        var owner = await ClientAsync("owner@one.local", "one-digital");
        var item = await CreateAsync(owner);
        foreach (var request in new[] { Update(item) with { EntryLimit = 0 }, Update(item) with { NumberOfReserveWinners = -1 },
            Update(item) with { MinimumAge = 121 }, Update(item) with { AllowedCountryCodes = ["Cyprus"] },
            Update(item) with { EndsAt = item.StartsAt.AddDays(-1) } })
            Assert.False((await owner.PatchAsJsonAsync($"/api/competitions/{item.Id}", request)).IsSuccessStatusCode);
        Assert.Equal(item.Name, (await owner.GetFromJsonAsync<CompetitionResponse>($"/api/competitions/{item.Id}"))!.Name);
    }

    [Fact]
    public async Task Viewer_and_other_tenant_cannot_edit_a_competition()
    {
        await _factory.SeedAsync();
        var owner = await ClientAsync("owner@one.local", "one-digital");
        var item = await CreateAsync(owner);
        var foreign = await ClientAsync("owner@omega.local", "omega-tv");
        Assert.False((await foreign.PatchAsJsonAsync($"/api/competitions/{item.Id}", Update(item))).IsSuccessStatusCode);
        var admin = await ClientAsync("admin@onecompetitions.local", "omega-tv");
        var omegaItem = await CreateAsync(admin);
        Assert.False((await foreign.PatchAsJsonAsync($"/api/competitions/{omegaItem.Id}", Update(omegaItem))).IsSuccessStatusCode);
    }

    [Fact]
    public async Task A_frozen_draw_prevents_settings_changes_even_if_status_is_closed()
    {
        await _factory.SeedAsync();
        var owner = await ClientAsync("owner@one.local", "one-digital");
        var item = await CreateAsync(owner);
        var fieldRequest = new UpsertCompetitionFieldRequest("email", "Email", "Email", null, null, true, 1, "{}", "[]", false, true, true);
        var fieldResponse = await owner.PostAsJsonAsync($"/api/competitions/{item.Id}/fields", fieldRequest);
        fieldResponse.EnsureSuccessStatusCode();
        var field = (await fieldResponse.Content.ReadFromJsonAsync<CompetitionFieldResponse>())!;
        (await owner.PatchAsJsonAsync($"/api/competitions/{item.Id}/email-verification", new UpdateEmailVerificationRequest(true))).EnsureSuccessStatusCode();
        (await owner.PostAsJsonAsync($"/api/competitions/{item.Id}/close", "Prepare draw")).EnsureSuccessStatusCode();
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Draws.Add(new Draw { Id = Guid.NewGuid(), TenantId = item.TenantId, CompetitionId = item.Id,
                Status = DrawStatus.Prepared, DrawReference = $"TEST-{Guid.NewGuid():N}", CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
        var response = await owner.PatchAsJsonAsync($"/api/competitions/{item.Id}", Update(item));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("locked", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PatchAsJsonAsync($"/api/competitions/{item.Id}/email-verification", new UpdateEmailVerificationRequest(false))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PatchAsJsonAsync($"/api/competitions/{item.Id}/fields/{field.Id}", fieldRequest with { Label = "Changed" })).StatusCode);
        var reset = await owner.PostAsync("/api/competitions/email-verification/disable-all", null);
        reset.EnsureSuccessStatusCode();
        Assert.True((await reset.Content.ReadFromJsonAsync<EmailVerificationResetResponse>())!.LockedCompetitions >= 1);
        Assert.True((await owner.GetFromJsonAsync<CompetitionResponse>($"/api/competitions/{item.Id}"))!.RequiresEmailVerification);
    }

    private async Task<HttpClient> ClientAsync(string email, string tenant)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "DevelopmentOnly!ChangeMe123"));
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        client.DefaultRequestHeaders.Host = $"{tenant}.competitions.local";
        return client;
    }

    private static async Task<CompetitionResponse> CreateAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/competitions", new CreateCompetitionRequest("Original competition", $"edit-{Guid.NewGuid():N}",
            "Original description", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), null, 1, 1, 1, false));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CompetitionResponse>())!;
    }

    private static UpdateCompetitionRequest Update(CompetitionResponse item) => new(item.Name, item.Description,
        item.StartsAt, item.EndsAt, item.EntryLimit, item.PerParticipantEntryLimit, item.NumberOfWinners,
        item.NumberOfReserveWinners, item.RequiresManualApproval, item.MinimumAge, item.AllowedCountryCodes,
        item.RequiresEmailVerification, item.RequiresPhoneVerification, item.AllowedParticipantAuthProviders);
}
