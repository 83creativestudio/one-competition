using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Assets;

public sealed class Asset : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid? CompetitionId { get; set; }
    public Guid? EntryId { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string OriginalFilename { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public string ScanStatus { get; set; } = "Pending";
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
