using Microsoft.Extensions.Configuration;

namespace OneCompetitions.Infrastructure.Tenancy;

/// <summary>Identifies shared platform hosts and the platform-owned subdomain namespace.</summary>
public static class PlatformHostnames
{
    /// <summary>Shared hosts resolve workspaces through membership or explicit tenant routes.</summary>
    public static bool IsSharedHost(string hostname, IConfiguration configuration) =>
        SharedHosts(configuration).Contains(hostname.TrimEnd('.'), StringComparer.OrdinalIgnoreCase);

    /// <summary>Platform-owned names must not be registered as customer custom domains.</summary>
    public static bool IsReserved(string hostname, IConfiguration configuration)
    {
        var value = hostname.TrimEnd('.');
        var baseDomain = Normalize(configuration["PLATFORM_BASE_DOMAIN"] ?? "competitions.local")!;
        return IsSharedHost(value, configuration) || value.EndsWith($".{baseDomain}", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> SharedHosts(IConfiguration configuration) =>
        new[] { configuration["PLATFORM_BASE_DOMAIN"] ?? "competitions.local", configuration["PLATFORM_AUTH_DOMAIN"],
            configuration["PLATFORM_QR_DOMAIN"], configuration["PLATFORM_VERIFY_DOMAIN"] }
        .Select(Normalize).OfType<string>();

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? uri.IdnHost.TrimEnd('.').ToLowerInvariant()
            : value.Trim().TrimEnd('.', '/').ToLowerInvariant();
    }
}
