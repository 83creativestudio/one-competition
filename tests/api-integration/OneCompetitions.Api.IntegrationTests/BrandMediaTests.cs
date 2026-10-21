using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneCompetitions.Application.Jobs;
using OneCompetitions.Domain.Privacy;
using OneCompetitions.Infrastructure.Persistence;
using OneCompetitions.Contracts.Assets;
using OneCompetitions.Contracts.Auth;
using OneCompetitions.Contracts.Branding;
using OneCompetitions.Contracts.Competitions;
using OneCompetitions.Contracts.Entries;

namespace OneCompetitions.Api.IntegrationTests;

public sealed class BrandMediaTests
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9Zkz8AAAAASUVORK5CYII=");

    [Fact]
    public async Task Brand_logos_are_validated_and_clients_cannot_assign_foreign_assets_or_brands()
    {
        using var factory = new TestApplicationFactory(); await factory.SeedAsync();
        var owner = await Login(factory, "owner@one.local");
        var logo = await Upload(owner, null, "image/png", Png);
        var brand = await Brand(owner, "Client brand", logo);
        var anonymous = factory.CreateClient();
        (await anonymous.GetAsync($"/c/one-digital/api/public/media/{logo}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/c/omega-tv/api/public/media/{logo}")).StatusCode);
        var publicBrand = await anonymous.GetFromJsonAsync<PublicBrandResponse>($"/c/one-digital/api/public/brands/{brand.Id}");
        Assert.Equal(logo, publicBrand!.Brand.LogoAssetId);
        Assert.Empty(publicBrand.Competitions);
        var admin = await Login(factory, "admin@onecompetitions.local");
        admin.DefaultRequestHeaders.Add("X-One-Tenant", "omega-tv");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/brands", BrandRequest("Invalid brand", logo))).StatusCode);
        var otherCompetition = await Competition(admin, "other-client");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/competitions/{otherCompetition.Id}/brand", new SetCompetitionBrandRequest(brand.Id))).StatusCode);
        var privateImage = await Upload(owner, null, "image/png", Png);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/c/one-digital/api/public/media/{privateImage}")).StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            foreach (var asset in await db.Assets.IgnoreQueryFilters().Where(x => x.Id == logo || x.Id == privateImage).ToListAsync())
                asset.CreatedAt = DateTimeOffset.UtcNow.AddDays(-10);
            db.DataRetentionPolicies.Add(new DataRetentionPolicy { Id = Guid.NewGuid(), TenantId = brand.TenantId, DeleteUploadsAfterDays = 1 });
            await db.SaveChangesAsync();
            await scope.ServiceProvider.GetRequiredService<IPlatformJobProcessor>().RunOnceAsync(CancellationToken.None);
            Assert.Null((await db.Assets.IgnoreQueryFilters().SingleAsync(x => x.Id == logo)).DeletedAt);
            Assert.NotNull((await db.Assets.IgnoreQueryFilters().SingleAsync(x => x.Id == privateImage)).DeletedAt);
        }
    }

    [Fact]
    public async Task Published_campaigns_render_only_published_media_and_brand_directory_hides_drafts()
    {
        using var factory = new TestApplicationFactory(); await factory.SeedAsync();
        var owner = await Login(factory, "owner@one.local");
        var brand = await Brand(owner, "Media brand", null);
        var competition = await Competition(owner, "media-campaign");
        var draft = await Competition(owner, "private-draft");
        foreach (var id in new[] { competition.Id, draft.Id })
            (await owner.PutAsJsonAsync($"/api/competitions/{id}/brand", new SetCompetitionBrandRequest(brand.Id))).EnsureSuccessStatusCode();
        var image = await Upload(owner, competition.Id, "image/png", Png);
        var video = await Upload(owner, competition.Id, "video/mp4", [0, 0, 0, 16, 102, 116, 121, 112, 105, 115, 111, 109, 0, 0, 0, 0]);
        var page = Page("Published title", new object[] {
            new { id = "text", type = "text", settings = new { text = "Published story" } },
            new { id = "image", type = "image", settings = new { assetId = image.ToString(), alt = "Prize photo" } },
            new { id = "video", type = "video", settings = new { assetId = video.ToString() } },
            new { id = "youtube", type = "video", settings = new { youtubeUrl = "https://youtu.be/dQw4w9WgXcQ" } }
        });
        (await owner.PutAsJsonAsync($"/api/competitions/{competition.Id}/page", page)).EnsureSuccessStatusCode();
        var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/c/one-digital/api/public/media/{image}")).StatusCode);
        (await owner.PostAsync($"/api/competitions/{competition.Id}/publish", null)).EnsureSuccessStatusCode();
        var publicPage = await anonymous.GetFromJsonAsync<PublicCompetitionResponse>("/c/one-digital/api/public/competitions/media-campaign");
        Assert.Equal(brand.Id, publicPage!.Brand!.Id);
        Assert.Contains("Published story", publicPage.Page!.LayoutJson);
        var imageResponse = await anonymous.GetAsync($"/c/one-digital/api/public/media/{image}");
        imageResponse.EnsureSuccessStatusCode();
        Assert.Equal("image/png", imageResponse.Content.Headers.ContentType!.MediaType);
        var videoResponse = await anonymous.GetAsync($"/c/one-digital/api/public/media/{video}");
        videoResponse.EnsureSuccessStatusCode();
        Assert.Equal("video/mp4", videoResponse.Content.Headers.ContentType!.MediaType);
        var newImage = await Upload(owner, competition.Id, "image/png", Png);
        var updated = Page("Draft title", new object[] { new { id = "new-image", type = "image", settings = new { assetId = newImage.ToString() } } });
        (await owner.PutAsJsonAsync($"/api/competitions/{competition.Id}/page", updated)).EnsureSuccessStatusCode();
        publicPage = await anonymous.GetFromJsonAsync<PublicCompetitionResponse>("/c/one-digital/api/public/competitions/media-campaign");
        Assert.Equal("Published title", publicPage!.Page!.Title);
        Assert.Equal("Draft title", (await owner.GetFromJsonAsync<CompetitionPageResponse>($"/api/competitions/{competition.Id}/page"))!.Title);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/c/one-digital/api/public/media/{newImage}")).StatusCode);
        var directory = await anonymous.GetFromJsonAsync<PublicBrandResponse>($"/c/one-digital/api/public/brands/{brand.Id}");
        Assert.Single(directory!.Competitions);
        Assert.Equal("media-campaign", directory.Competitions[0].Slug);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.DeleteAsync($"/api/brands/{brand.Id}")).StatusCode);
        (await owner.PostAsJsonAsync($"/api/competitions/{competition.Id}/close", "Test closing")).EnsureSuccessStatusCode();
        (await owner.PostAsync($"/api/competitions/{competition.Id}/publish", null)).EnsureSuccessStatusCode();
        Assert.Equal("Closed", (await owner.GetFromJsonAsync<CompetitionResponse>($"/api/competitions/{competition.Id}"))!.Status);
        (await anonymous.GetAsync($"/c/one-digital/api/public/media/{newImage}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/c/one-digital/api/public/media/{image}")).StatusCode);
    }

    [Fact]
    public async Task Page_validation_rejects_untrusted_video_hosts_and_foreign_competition_uploads()
    {
        using var factory = new TestApplicationFactory(); await factory.SeedAsync();
        var owner = await Login(factory, "owner@one.local");
        var first = await Competition(owner, "first-media");
        var second = await Competition(owner, "second-media");
        var image = await Upload(owner, second.Id, "image/png", Png);
        foreach (var url in new[] { "https://attacker.test/watch?v=dQw4w9WgXcQ", "javascript:alert(1)", "https://youtube.com/watch?v=bad" })
        {
            var request = Page("Invalid", new object[] { new { id = "video", type = "video", settings = new { youtubeUrl = url } } });
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync($"/api/competitions/{first.Id}/page", request)).StatusCode);
        }
        var foreign = Page("Invalid", new object[] { new { id = "image", type = "image", settings = new { assetId = image.ToString() } } });
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync($"/api/competitions/{first.Id}/page", foreign)).StatusCode);
    }

    private static UpsertCompetitionPageRequest Page(string title, object[] blocks) => new("en", title, title, "Campaign summary", JsonSerializer.Serialize(new { schemaVersion = 1, blocks }));
    private static UpsertBrandProfileRequest BrandRequest(string name, Guid? logo) => new(name, "#006c5b", "#171918", "#d14b30", "#ffffff", "#171918", "Inter", "Inter", "Solid", 6, null, null, null, true, null, logo);
    private static async Task<BrandProfileResponse> Brand(HttpClient client, string name, Guid? logo)
    {
        var response = await client.PostAsJsonAsync("/api/brands", BrandRequest(name, logo));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BrandProfileResponse>())!;
    }
    private static async Task<HttpClient> Login(TestApplicationFactory factory, string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "DevelopmentOnly!ChangeMe123"));
        response.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken);
        return client;
    }
    private static async Task<CompetitionResponse> Competition(HttpClient client, string slug)
    {
        var response = await client.PostAsJsonAsync("/api/competitions", new CreateCompetitionRequest(slug, slug, null, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(2), null, 1, 1, 0, false));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CompetitionResponse>())!;
    }
    private static async Task<Guid> Upload(HttpClient client, Guid? competition, string mime, byte[] content)
    {
        var response = await client.PostAsJsonAsync("/api/assets/uploads", new CreateAssetUploadRequest(competition, null, "CampaignMedia", mime == "video/mp4" ? "test.mp4" : "test.png", mime, content.Length));
        response.EnsureSuccessStatusCode();
        var upload = (await response.Content.ReadFromJsonAsync<AssetUploadResponse>())!;
        (await client.PutAsync(upload.UploadUrl, new ByteArrayContent(content))).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/assets/{upload.AssetId}/complete", null)).EnsureSuccessStatusCode();
        return upload.AssetId;
    }
}
