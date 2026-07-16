namespace OneCompetitions.Contracts.Assets;

public sealed record CreateAssetUploadRequest(Guid? CompetitionId, Guid? EntryId, string AssetType, string Filename, string MimeType, long SizeBytes);
public sealed record AssetUploadResponse(Guid AssetId, string StorageKey, string UploadUrl, DateTimeOffset ExpiresAt);
public sealed record AssetResponse(Guid Id, Guid? CompetitionId, string AssetType, string OriginalFilename, string MimeType, long SizeBytes, string Sha256Hash, string ScanStatus, DateTimeOffset CreatedAt);
