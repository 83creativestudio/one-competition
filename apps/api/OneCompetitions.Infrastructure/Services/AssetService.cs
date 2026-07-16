using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OneCompetitions.Application.Storage;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Contracts.Assets;
using OneCompetitions.Domain.Assets;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class AssetService(
    AppDbContext dbContext,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    IObjectStorage storage,
    IVirusScanner virusScanner,
    IConfiguration configuration,
    IHostEnvironment environment)
    : TenantScopedServiceBase(dbContext, tenantContext, userManager), IAssetService
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTypes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = [".jpg", ".jpeg"],
        ["image/png"] = [".png"],
        ["image/webp"] = [".webp"],
        ["application/pdf"] = [".pdf"],
        ["video/mp4"] = [".mp4"]
    };

    public async Task<AssetUploadResponse> CreateUploadAsync(Guid userId, CreateAssetUploadRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        Validate(request);
        await ValidateTenantReferencesAsync(request, cancellationToken);
        var id = Guid.NewGuid();
        var filename = Path.GetFileName(request.Filename);
        var key = $"tenant/{TenantContext.TenantId:N}/{(request.CompetitionId is null ? "shared" : $"competition/{request.CompetitionId:N}")}/assets/{id:N}{Path.GetExtension(filename).ToLowerInvariant()}";
        var asset = new Asset
        {
            Id = id,
            TenantId = TenantContext.TenantId,
            CompetitionId = request.CompetitionId,
            EntryId = request.EntryId,
            AssetType = request.AssetType,
            OriginalFilename = filename,
            StorageKey = key,
            MimeType = request.MimeType.ToLowerInvariant(),
            SizeBytes = request.SizeBytes,
            ScanStatus = "PendingUpload",
            CreatedAt = DateTimeOffset.UtcNow
        };
        DbContext.Assets.Add(asset);
        await DbContext.SaveChangesAsync(cancellationToken);
        var expires = DateTimeOffset.UtcNow.AddMinutes(15);
        var url = IsLocalStorage()
            ? $"/api/assets/{asset.Id}/local-content"
            : await storage.CreateUploadUrlAsync(key, asset.MimeType, expires - DateTimeOffset.UtcNow, cancellationToken);
        return new AssetUploadResponse(asset.Id, key, url, expires);
    }

    public async Task StoreLocalUploadAsync(Guid userId, Guid assetId, Stream content, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        EnsureLocalStorage();
        var asset = await GetAsync(assetId, cancellationToken);
        await storage.PutAsync(asset.StorageKey, content, asset.MimeType, cancellationToken);
    }

    public async Task<AssetResponse> CompleteUploadAsync(Guid userId, Guid assetId, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var asset = await GetAsync(assetId, cancellationToken);
        if (!await storage.ExistsAsync(asset.StorageKey, cancellationToken)) throw new InvalidOperationException("Uploaded object was not found.");
        await using var content = await storage.OpenReadAsync(asset.StorageKey, cancellationToken);
        if (content.CanSeek && content.Length != asset.SizeBytes) throw new InvalidOperationException("Uploaded object size does not match its declaration.");
        await ValidateSignatureAsync(content, asset.MimeType, cancellationToken);
        if (content.CanSeek) content.Position = 0;
        var scan = await virusScanner.ScanAsync(content, asset.OriginalFilename, cancellationToken);
        if (content.CanSeek) content.Position = 0;
        asset.Sha256Hash = Convert.ToHexString(await SHA256.HashDataAsync(content, cancellationToken)).ToLowerInvariant();
        asset.ScanStatus = scan.IsClean ? "Clean" : "Rejected";
        if (!scan.IsClean)
        {
            await storage.DeleteAsync(asset.StorageKey, cancellationToken);
            asset.DeletedAt = DateTimeOffset.UtcNow;
            await DbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException($"Upload was rejected by malware scanning: {scan.ThreatName ?? "threat detected"}.");
        }

        await DbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(asset);
    }

    public async Task<string> CreateDownloadUrlAsync(Guid userId, Guid assetId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        var asset = await GetAsync(assetId, cancellationToken);
        if (asset.ScanStatus != "Clean") throw new InvalidOperationException("Asset is not available for download.");
        return string.Equals(configuration["OBJECT_STORAGE_PROVIDER"], "local", StringComparison.OrdinalIgnoreCase)
            ? $"/api/assets/{asset.Id}/local-content"
            : await storage.CreateDownloadUrlAsync(asset.StorageKey, TimeSpan.FromMinutes(10), cancellationToken);
    }

    public async Task<(Stream Content, string ContentType, string Filename)> OpenLocalDownloadAsync(Guid userId, Guid assetId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        EnsureLocalStorage();
        var asset = await GetAsync(assetId, cancellationToken);
        return (await storage.OpenReadAsync(asset.StorageKey, cancellationToken), asset.MimeType, asset.OriginalFilename);
    }

    private async Task<Asset> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await DbContext.Assets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new InvalidOperationException("Asset was not found.");

    private async Task ValidateTenantReferencesAsync(CreateAssetUploadRequest request, CancellationToken cancellationToken)
    {
        if (request.CompetitionId is not null && !await DbContext.Competitions.AnyAsync(x => x.Id == request.CompetitionId, cancellationToken))
            throw new InvalidOperationException("Competition was not found in the current tenant.");
        if (request.EntryId is not null)
        {
            var entry = await DbContext.CompetitionEntries.SingleOrDefaultAsync(x => x.Id == request.EntryId, cancellationToken)
                ?? throw new InvalidOperationException("Entry was not found in the current tenant.");
            if (request.CompetitionId is not null && entry.CompetitionId != request.CompetitionId)
                throw new InvalidOperationException("Entry does not belong to the selected competition.");
        }
    }

    private void EnsureLocalStorage()
    {
        if (!IsLocalStorage())
            throw new InvalidOperationException("Local storage endpoint is disabled.");
    }

    private bool IsLocalStorage() => string.Equals(configuration["OBJECT_STORAGE_PROVIDER"], "local", StringComparison.OrdinalIgnoreCase)
        || (string.IsNullOrWhiteSpace(configuration["OBJECT_STORAGE_PROVIDER"]) && (environment.IsDevelopment() || environment.IsEnvironment("Testing")));

    private static void Validate(CreateAssetUploadRequest request)
    {
        if (request.SizeBytes <= 0 || request.SizeBytes > 25 * 1024 * 1024) throw new InvalidOperationException("File size must be between 1 byte and 25 MB.");
        if (!AllowedTypes.TryGetValue(request.MimeType, out var extensions) || !extensions.Contains(Path.GetExtension(request.Filename), StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("File extension and MIME type are not allowed.");
        if (request.MimeType.Equals("video/mp4", StringComparison.OrdinalIgnoreCase) && request.SizeBytes > 25 * 1024 * 1024)
            throw new InvalidOperationException("Video exceeds the configured upload limit.");
    }

    private static async Task ValidateSignatureAsync(Stream content, string mimeType, CancellationToken cancellationToken)
    {
        if (!content.CanSeek) throw new InvalidOperationException("Uploaded object cannot be inspected safely.");
        var header = new byte[Math.Min(16, (int)content.Length)];
        _ = await content.ReadAsync(header, cancellationToken);
        content.Position = 0;
        var valid = mimeType.ToLowerInvariant() switch
        {
            "image/jpeg" => header.Length >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff,
            "image/png" => header.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }),
            "image/webp" => header.Length >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            "application/pdf" => header.AsSpan().StartsWith("%PDF-"u8),
            "video/mp4" => header.Length >= 12 && header.AsSpan(4, 4).SequenceEqual("ftyp"u8),
            _ => false
        };
        if (!valid) throw new InvalidOperationException("Uploaded content does not match its declared MIME type.");
    }

    private static AssetResponse ToResponse(Asset asset) => new(asset.Id, asset.CompetitionId, asset.AssetType, asset.OriginalFilename, asset.MimeType, asset.SizeBytes, asset.Sha256Hash, asset.ScanStatus, asset.CreatedAt);
}
