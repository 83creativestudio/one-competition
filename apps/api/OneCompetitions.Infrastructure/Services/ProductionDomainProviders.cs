using System.Net.Http.Json;
using System.Text.Json;
using DnsClient;
using DnsClient.Protocol;
using Microsoft.Extensions.Configuration;
using OneCompetitions.Application.Domains;

namespace OneCompetitions.Infrastructure.Services;

public sealed class DnsDomainVerificationProvider : IDomainVerificationProvider
{
    private readonly LookupClient _client = new(new LookupClientOptions { UseCache = false, Timeout = TimeSpan.FromSeconds(5) });

    public async Task<DomainVerificationResult> VerifyAsync(string hostname, string token, CancellationToken cancellationToken)
    {
        var expected = $"one-competitions-verify={token}";
        foreach (var name in new[] { hostname, $"_one-competitions.{hostname}" })
        {
            try
            {
                var result = await _client.QueryAsync(name, QueryType.TXT, cancellationToken: cancellationToken);
                if (result.Answers.TxtRecords().SelectMany(x => x.Text).Any(x => string.Equals(x, expected, StringComparison.Ordinal)))
                {
                    return new DomainVerificationResult(true, null, expected);
                }
            }
            catch (DnsResponseException)
            {
                // Try both supported TXT record locations before reporting the failure.
            }
        }

        return new DomainVerificationResult(false, $"TXT record was not found on {hostname} or _one-competitions.{hostname}.", expected);
    }
}

public sealed class CloudflareSslProvisioningProvider(HttpClient client, IConfiguration configuration) : ISslProvisioningProvider
{
    public async Task<SslProvisioningResult> ProvisionAsync(string hostname, CancellationToken cancellationToken)
    {
        var zoneId = configuration["CLOUDFLARE_ZONE_ID"] ?? throw new InvalidOperationException("CLOUDFLARE_ZONE_ID is required.");
        var token = configuration["CLOUDFLARE_API_TOKEN"] ?? throw new InvalidOperationException("CLOUDFLARE_API_TOKEN is required.");
        using var lookup = new HttpRequestMessage(HttpMethod.Get, $"https://api.cloudflare.com/client/v4/zones/{zoneId}/custom_hostnames?hostname={Uri.EscapeDataString(hostname)}");
        lookup.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var lookupResponse = await client.SendAsync(lookup, cancellationToken);
        var lookupJson = await lookupResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!lookupResponse.IsSuccessStatusCode)
            return Failure(lookupResponse, lookupJson);
        using (var lookupDocument = JsonDocument.Parse(lookupJson))
        {
            var existing = lookupDocument.RootElement.GetProperty("result");
            if (existing.GetArrayLength() > 0) return ReadStatus(existing[0]);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.cloudflare.com/client/v4/zones/{zoneId}/custom_hostnames")
        {
            Content = JsonContent.Create(new
            {
                hostname,
                ssl = new { method = "txt", type = "dv", settings = new { min_tls_version = "1.2" } }
            })
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) return Failure(response, json);

        using var document = JsonDocument.Parse(json);
        return ReadStatus(document.RootElement.GetProperty("result"));
    }

    private static SslProvisioningResult ReadStatus(JsonElement result)
    {
        var hostnameStatus = result.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : null;
        var ssl = result.TryGetProperty("ssl", out var sslElement) ? sslElement : default;
        var sslStatus = ssl.ValueKind == JsonValueKind.Object && ssl.TryGetProperty("status", out var sslStatusElement) ? sslStatusElement.GetString() : null;
        var active = string.Equals(hostnameStatus, "active", StringComparison.OrdinalIgnoreCase) && string.Equals(sslStatus, "active", StringComparison.OrdinalIgnoreCase);
        DateTimeOffset? expiresAt = null;
        if (ssl.ValueKind == JsonValueKind.Object && ssl.TryGetProperty("certificates", out var certificates) && certificates.ValueKind == JsonValueKind.Array && certificates.GetArrayLength() > 0
            && certificates[0].TryGetProperty("expires_on", out var expiry) && expiry.TryGetDateTimeOffset(out var parsed)) expiresAt = parsed;
        return new SslProvisioningResult(active, active ? null : $"Cloudflare hostname/SSL status is {hostnameStatus ?? "pending"}/{sslStatus ?? "pending"}.", expiresAt);
    }

    private static SslProvisioningResult Failure(HttpResponseMessage response, string body) =>
        new(false, $"Cloudflare returned {(int)response.StatusCode}: {body[..Math.Min(body.Length, 500)]}", null);
}

public sealed class DevelopmentSslProvisioningProvider : ISslProvisioningProvider
{
    public Task<SslProvisioningResult> ProvisionAsync(string hostname, CancellationToken cancellationToken) =>
        Task.FromResult(new SslProvisioningResult(true, null, DateTimeOffset.UtcNow.AddYears(1)));
}
