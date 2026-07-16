using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using OneCompetitions.Application.Storage;

namespace OneCompetitions.Infrastructure.Services;

public sealed class S3ObjectStorage(IAmazonS3 client, IConfiguration configuration) : IObjectStorage
{
    private string Bucket => configuration["OBJECT_STORAGE_BUCKET"]
        ?? throw new InvalidOperationException("OBJECT_STORAGE_BUCKET is required.");

    public Task<string> CreateUploadUrlAsync(string storageKey, string contentType, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = Bucket,
            Key = storageKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(lifetime),
            ContentType = contentType
        };
        return Task.FromResult(client.GetPreSignedURL(request));
    }

    public Task<string> CreateDownloadUrlAsync(string storageKey, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = Bucket,
            Key = storageKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(lifetime)
        };
        return Task.FromResult(client.GetPreSignedURL(request));
    }

    public async Task PutAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken)
    {
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Bucket,
            Key = storageKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        }, cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        using var response = await client.GetObjectAsync(Bucket, storageKey, cancellationToken);
        var output = new MemoryStream();
        await response.ResponseStream.CopyToAsync(output, cancellationToken);
        output.Position = 0;
        return output;
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken) =>
        client.DeleteObjectAsync(Bucket, storageKey, cancellationToken);

    public async Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken)
    {
        try
        {
            await client.GetObjectMetadataAsync(Bucket, storageKey, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}

public sealed class LocalObjectStorage(IConfiguration configuration) : IObjectStorage
{
    private string Root => Path.GetFullPath(configuration["LOCAL_STORAGE_PATH"] ?? Path.Combine(Path.GetTempPath(), "one-competitions-storage"));

    public Task<string> CreateUploadUrlAsync(string storageKey, string contentType, TimeSpan lifetime, CancellationToken cancellationToken) =>
        Task.FromResult($"/api/assets/local-upload?key={Uri.EscapeDataString(storageKey)}");

    public Task<string> CreateDownloadUrlAsync(string storageKey, TimeSpan lifetime, CancellationToken cancellationToken) =>
        Task.FromResult($"/api/local-storage?key={Uri.EscapeDataString(storageKey)}");

    public async Task PutAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken)
    {
        var path = Resolve(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var output = File.Create(path);
        await content.CopyToAsync(output, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(File.OpenRead(Resolve(storageKey)));

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        var path = Resolve(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken) => Task.FromResult(File.Exists(Resolve(storageKey)));

    private string Resolve(string storageKey)
    {
        var path = Path.GetFullPath(Path.Combine(Root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(Root, StringComparison.Ordinal)) throw new InvalidOperationException("Storage key is outside the configured root.");
        return path;
    }
}
