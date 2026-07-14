using System.Net.Http.Headers;
using System.Net.Http.Json;
using OneCompetitions.Contracts.Auth;
using OneCompetitions.Contracts.Campaigns;
using OneCompetitions.Contracts.Competitions;
using OneCompetitions.Contracts.Draws;
using OneCompetitions.Contracts.Entries;
using OneCompetitions.Contracts.Exports;
using OneCompetitions.Contracts.Qr;
using OneCompetitions.Contracts.Winners;

namespace OneCompetitions.Api.IntegrationTests;

public sealed class MvpWorkflowTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public MvpWorkflowTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Tenant_can_publish_accept_entries_track_qr_draw_winners_and_export()
    {
        await _factory.SeedAsync();
        var owner = await CreateClientAsync("owner@one.local");
        owner.DefaultRequestHeaders.Host = "one-digital.competitions.local";

        var competition = await CreateCompetitionAsync(owner);
        var field = await owner.PostAsJsonAsync($"/api/competitions/{competition.Id}/fields", new UpsertCompetitionFieldRequest("email", "Email", "Email", null, null, true, 1, null, null, false, true, true));
        field.EnsureSuccessStatusCode();
        var rules = await owner.PostAsJsonAsync($"/api/competitions/{competition.Id}/rules", new CreateRulesVersionRequest("en", "Rules", "Terms apply.", null));
        rules.EnsureSuccessStatusCode();
        var publish = await owner.PostAsync($"/api/competitions/{competition.Id}/publish", null);
        publish.EnsureSuccessStatusCode();

        var sourceResponse = await owner.PostAsJsonAsync($"/api/competitions/{competition.Id}/sources", new CreateCampaignSourceRequest("Poster", "Poster", "POSTER", null, null, null, null));
        sourceResponse.EnsureSuccessStatusCode();
        var source = await sourceResponse.Content.ReadFromJsonAsync<CampaignSourceResponse>() ?? throw new InvalidOperationException("Missing source.");

        var qrResponse = await owner.PostAsJsonAsync($"/api/competitions/{competition.Id}/qr-codes", new CreateQrCodeRequest(source.Id, $"https://one-digital.competitions.local/{competition.Slug}", null, null));
        qrResponse.EnsureSuccessStatusCode();
        var qr = await qrResponse.Content.ReadFromJsonAsync<QrCodeResponse>() ?? throw new InvalidOperationException("Missing QR.");

        var publicClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        publicClient.DefaultRequestHeaders.Host = "one-digital.competitions.local";
        var publicCompetition = await publicClient.GetFromJsonAsync<PublicCompetitionResponse>($"/api/public/competitions/{competition.Slug}");
        Assert.NotNull(publicCompetition);

        var scan = await publicClient.GetAsync($"/q/{qr.ShortCode}");
        Assert.True((int)scan.StatusCode is >= 300 and < 400);

        var entryOne = await SubmitEntryAsync(publicClient, competition.Slug, "entry-one@example.local", source.Id);
        var entryTwo = await SubmitEntryAsync(publicClient, competition.Slug, "entry-two@example.local", source.Id);
        Assert.Equal("Approved", entryOne.Status);
        Assert.Equal("Approved", entryTwo.Status);

        var analytics = await owner.GetFromJsonAsync<OneCompetitions.Contracts.Analytics.AnalyticsOverviewResponse>($"/api/competitions/{competition.Id}/analytics/overview");
        Assert.NotNull(analytics);
        Assert.True(analytics!.SubmittedEntries >= 2);

        var close = await owner.PostAsJsonAsync($"/api/competitions/{competition.Id}/close", "ready for draw");
        close.EnsureSuccessStatusCode();
        var preparedResponse = await owner.PostAsJsonAsync($"/api/competitions/{competition.Id}/draws/prepare", new PrepareDrawRequest(1, 1));
        preparedResponse.EnsureSuccessStatusCode();
        var draw = await preparedResponse.Content.ReadFromJsonAsync<DrawResponse>() ?? throw new InvalidOperationException("Missing draw.");
        Assert.Equal("AwaitingApproval", draw.Status);

        var admin = await CreateClientAsync("admin@onecompetitions.local");
        admin.DefaultRequestHeaders.Host = "one-digital.competitions.local";
        var approvedResponse = await admin.PostAsync($"/api/competitions/{competition.Id}/draws/{draw.Id}/approve", null);
        approvedResponse.EnsureSuccessStatusCode();

        var executedResponse = await owner.PostAsync($"/api/competitions/{competition.Id}/draws/{draw.Id}/execute", null);
        executedResponse.EnsureSuccessStatusCode();
        var executed = await executedResponse.Content.ReadFromJsonAsync<DrawResponse>() ?? throw new InvalidOperationException("Missing executed draw.");
        Assert.Equal("Completed", executed.Status);

        var results = await owner.GetFromJsonAsync<List<DrawResultResponse>>($"/api/competitions/{competition.Id}/draws/{draw.Id}/results");
        Assert.NotNull(results);
        Assert.Equal(2, results!.Count);
        Assert.Equal(2, results.Select(x => x.EntryId).Distinct().Count());

        var winners = await owner.GetFromJsonAsync<List<WinnerClaimResponse>>($"/api/competitions/{competition.Id}/winners");
        Assert.NotNull(winners);
        Assert.Equal(2, winners!.Count);

        var exportResponse = await owner.PostAsJsonAsync($"/api/competitions/{competition.Id}/exports", new CreateExportRequest("Entries", "Csv"));
        exportResponse.EnsureSuccessStatusCode();
        var export = await exportResponse.Content.ReadFromJsonAsync<ExportJobResponse>() ?? throw new InvalidOperationException("Missing export.");
        Assert.Equal("Completed", export.Status);
    }

    private async Task<CompetitionResponse> CreateCompetitionAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/competitions", new CreateCompetitionRequest(
            "Integration Prize Draw",
            $"integration-prize-draw-{Guid.NewGuid():N}",
            "Integration test",
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(1),
            null,
            1,
            1,
            1,
            false));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CompetitionResponse>() ?? throw new InvalidOperationException("Missing competition.");
    }

    private static async Task<EntryResponse> SubmitEntryAsync(HttpClient client, string slug, string email, Guid sourceId)
    {
        var response = await client.PostAsJsonAsync($"/api/public/competitions/{slug}/entries", new SubmitEntryRequest(
            email,
            null,
            "Integration",
            "Participant",
            "en",
            Guid.NewGuid().ToString("N"),
            sourceId,
            [new EntryAnswerRequest("email", email)],
            []));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EntryResponse>() ?? throw new InvalidOperationException("Missing entry.");
    }

    private async Task<HttpClient> CreateClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "DevelopmentOnly!ChangeMe123"));
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>() ?? throw new InvalidOperationException("Missing auth.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }
}
