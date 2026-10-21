using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OneCompetitions.Contracts.Competitions;
using OneCompetitions.Domain.Competitions;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

internal static class CompetitionPageContent
{
    public static UpsertCompetitionPageRequest? Published(CompetitionPage page) =>
        page.PublishedContentJson is not null
            ? JsonSerializer.Deserialize<UpsertCompetitionPageRequest>(page.PublishedContentJson)
            : page.PublishedAt is not null
                ? new(page.LanguageCode, page.Title, page.SeoTitle, page.SeoDescription, page.LayoutJson) : null;

    public static string Snapshot(CompetitionPage page) => JsonSerializer.Serialize(
        new UpsertCompetitionPageRequest(page.LanguageCode, page.Title, page.SeoTitle, page.SeoDescription, page.LayoutJson));

    public static IReadOnlyList<Guid> MediaIds(string layout)
    {
        using var json = JsonDocument.Parse(layout);
        if (!json.RootElement.TryGetProperty("blocks", out var blocks)) return [];
        return blocks.EnumerateArray().Where(x => !x.TryGetProperty("hidden", out var hidden) || hidden.ValueKind != JsonValueKind.True)
            .Where(x => x.TryGetProperty("settings", out _))
            .SelectMany(x => x.GetProperty("settings").EnumerateObject())
            .Where(x => x.Name is "assetId" or "backgroundAssetId" && x.Value.ValueKind == JsonValueKind.String)
            .Select(x => Guid.TryParse(x.Value.GetString(), out var id) ? id : Guid.Empty).Where(x => x != Guid.Empty).ToList();
    }

    public static async Task ValidateAsync(AppDbContext db, Guid tenantId, Guid competitionId, UpsertCompetitionPageRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 220 || request.SeoTitle?.Length > 220 || request.SeoDescription?.Length > 500)
            throw new InvalidOperationException("A page title of up to 220 characters is required; descriptions may contain up to 500 characters.");
        if (request.LayoutJson.Length > 200_000) throw new InvalidOperationException("Page content is too large.");
        using var document = JsonDocument.Parse(request.LayoutJson);
        var root = document.RootElement;
        if (!root.TryGetProperty("schemaVersion", out var version) || version.GetInt32() != 1
            || !root.TryGetProperty("blocks", out var blocks) || blocks.ValueKind != JsonValueKind.Array || blocks.GetArrayLength() > 40)
            throw new InvalidOperationException("Page content must contain up to 40 blocks using schema version 1.");
        var ids = new HashSet<string>();
        foreach (var block in blocks.EnumerateArray())
        {
            if (!block.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String || !ids.Add(id.GetString()!)
                || !block.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String
                || !block.TryGetProperty("settings", out var settings) || settings.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("Every block needs a unique ID, a type and settings.");
            var kind = type.GetString()!.ToLowerInvariant();
            foreach (var setting in settings.EnumerateObject())
                if (setting.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                    throw new InvalidOperationException("Block settings must contain text values.");
            if (kind is not ("hero" or "text" or "description" or "prize" or "image" or "video" or "entryform" or "footer" or "countdown"))
                throw new InvalidOperationException("This page block type is not supported.");
            if (kind == "video" && settings.TryGetProperty("youtubeUrl", out var youtube) && !string.IsNullOrWhiteSpace(youtube.GetString()))
            {
                if (!Uri.TryCreate(youtube.GetString(), UriKind.Absolute, out var uri) || uri.Scheme != "https"
                    || uri.Host is not ("youtube.com" or "www.youtube.com" or "youtu.be" or "m.youtube.com")
                    || uri.UserInfo.Length > 0)
                    throw new InvalidOperationException("Use an HTTPS YouTube video URL.");
                var videoId = uri.Host == "youtu.be" ? uri.AbsolutePath.Trim('/') :
                    uri.AbsolutePath.StartsWith("/shorts/") || uri.AbsolutePath.StartsWith("/embed/") ? uri.Segments.Last().Trim('/') :
                    Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query)["v"].ToString();
                if (!Regex.IsMatch(videoId, "^[a-zA-Z0-9_-]{11}$")) throw new InvalidOperationException("The YouTube video URL is invalid.");
            }
            foreach (var media in settings.EnumerateObject().Where(x => x.Name is "assetId" or "backgroundAssetId"))
            {
                if (media.Value.ValueKind == JsonValueKind.Null || media.Value.GetString() == "") continue;
                if (!Guid.TryParse(media.Value.GetString(), out var assetId)) throw new InvalidOperationException("Media asset is invalid.");
                var asset = await db.Assets.SingleOrDefaultAsync(x => x.Id == assetId && x.TenantId == tenantId
                    && x.CompetitionId == competitionId && x.EntryId == null && x.DeletedAt == null && x.ScanStatus == "Clean", ct);
                if (asset is null || (kind == "video" ? asset.MimeType != "video/mp4" : !asset.MimeType.StartsWith("image/")))
                    throw new InvalidOperationException("Select a completed image or video upload belonging to this competition.");
            }
        }
    }
}
