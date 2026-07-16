namespace OneCompetitions.Application.Storage;

public interface IObjectStorage
{
    Task<string> CreateUploadUrlAsync(string storageKey, string contentType, TimeSpan lifetime, CancellationToken cancellationToken);
    Task<string> CreateDownloadUrlAsync(string storageKey, TimeSpan lifetime, CancellationToken cancellationToken);
    Task PutAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken);
}
