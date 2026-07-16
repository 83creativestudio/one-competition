using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OneCompetitions.Application.Entries;

namespace OneCompetitions.Infrastructure.Services;

public sealed class DevelopmentCaptchaProvider(IConfiguration configuration, IHostEnvironment environment) : ICaptchaProvider
{
    public Task<bool> ValidateAsync(string? token, string? remoteIpAddress, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            throw new InvalidOperationException("The development CAPTCHA provider cannot run outside development or testing.");
        var expected = configuration["DEVELOPMENT_CAPTCHA_TOKEN"] ?? (environment.IsEnvironment("Testing") ? "test-pass" : "development-pass");
        return Task.FromResult(CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(token ?? string.Empty),
            System.Text.Encoding.UTF8.GetBytes(expected)));
    }
}

public sealed class TurnstileCaptchaProvider(HttpClient client, IConfiguration configuration) : ICaptchaProvider
{
    private sealed record TurnstileResponse(bool Success, string? Hostname, string? Action, string[]? ErrorCodes);

    public async Task<bool> ValidateAsync(string? token, string? remoteIpAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        var secret = configuration["TURNSTILE_SECRET_KEY"] ?? throw new InvalidOperationException("TURNSTILE_SECRET_KEY is required.");
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["secret"] = secret,
            ["response"] = token,
            ["remoteip"] = remoteIpAddress ?? string.Empty
        });
        using var response = await client.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content, cancellationToken);
        if (!response.IsSuccessStatusCode) return false;
        var result = await response.Content.ReadFromJsonAsync<TurnstileResponse>(cancellationToken: cancellationToken);
        return result?.Success == true;
    }
}
