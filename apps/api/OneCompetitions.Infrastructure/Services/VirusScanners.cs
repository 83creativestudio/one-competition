using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneCompetitions.Application.Storage;

namespace OneCompetitions.Infrastructure.Services;

public sealed class DevelopmentVirusScanner(ILogger<DevelopmentVirusScanner> logger) : IVirusScanner
{
    public Task<VirusScanResult> ScanAsync(Stream content, string filename, CancellationToken cancellationToken)
    {
        logger.LogInformation("Development virus scanner accepted {Filename}", filename);
        return Task.FromResult(new VirusScanResult(true));
    }
}

public sealed class HttpVirusScanner(HttpClient client, IConfiguration configuration) : IVirusScanner
{
    public async Task<VirusScanResult> ScanAsync(Stream content, string filename, CancellationToken cancellationToken)
    {
        var endpoint = configuration["VIRUS_SCANNER_ENDPOINT"] ?? throw new InvalidOperationException("VIRUS_SCANNER_ENDPOINT is required.");
        using var form = new MultipartFormDataContent();
        var file = new StreamContent(content);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        form.Add(file, "file", filename);
        using var response = await client.PostAsync(endpoint, form, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ScannerResponse>(cancellationToken) ?? throw new InvalidOperationException("Virus scanner returned no result.");
        return new VirusScanResult(result.IsClean, result.ThreatName);
    }

    private sealed record ScannerResponse(bool IsClean, string? ThreatName);
}
