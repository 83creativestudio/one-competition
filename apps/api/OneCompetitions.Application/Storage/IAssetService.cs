using OneCompetitions.Contracts.Assets;

namespace OneCompetitions.Application.Storage;

public interface IAssetService
{
    Task<AssetUploadResponse> CreateUploadAsync(Guid userId, CreateAssetUploadRequest request, CancellationToken cancellationToken);
    Task<AssetResponse> CompleteUploadAsync(Guid userId, Guid assetId, CancellationToken cancellationToken);
    Task<string> CreateDownloadUrlAsync(Guid userId, Guid assetId, CancellationToken cancellationToken);
    Task StoreLocalUploadAsync(Guid userId, Guid assetId, Stream content, CancellationToken cancellationToken);
    Task<(Stream Content, string ContentType, string Filename)> OpenLocalDownloadAsync(Guid userId, Guid assetId, CancellationToken cancellationToken);
}
