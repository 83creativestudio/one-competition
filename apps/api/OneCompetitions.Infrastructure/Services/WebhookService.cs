using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OneCompetitions.Application.Billing;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Application.Webhooks;
using OneCompetitions.Contracts.Webhooks;
using OneCompetitions.Domain.Webhooks;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class WebhookService(
    AppDbContext dbContext,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    SecretProtector protector,
    IPlanLimitService planLimits,
    IHostEnvironment environment)
    : TenantScopedServiceBase(dbContext, tenantContext, userManager), IWebhookService
{
    public async Task<IReadOnlyList<WebhookEndpointResponse>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        return await DbContext.WebhookEndpoints.OrderBy(x => x.Url).Select(x => ToResponse(x)).ToListAsync(cancellationToken);
    }

    public async Task<WebhookEndpointCreatedResponse> CreateAsync(Guid userId, CreateWebhookEndpointRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        await planLimits.EnsureAllowedAsync(TenantContext.TenantId, "AllowWebhooks", 1, cancellationToken);
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Webhook URL must use HTTPS.");
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            await WebhookUrlValidator.EnsurePublicHttpsAsync(uri, cancellationToken);
        if (request.EventTypes.Count == 0 || request.EventTypes.Any(x => string.IsNullOrWhiteSpace(x)))
            throw new InvalidOperationException("At least one event type is required.");

        var secret = $"whsec_{Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant()}";
        var endpoint = new WebhookEndpoint
        {
            Id = Guid.NewGuid(), TenantId = TenantContext.TenantId, Url = uri.ToString(),
            SecretHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret))).ToLowerInvariant(),
            SecretCiphertext = protector.Protect(secret), EventTypesJson = JsonSerializer.Serialize(request.EventTypes.Distinct().Order()),
            IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        DbContext.WebhookEndpoints.Add(endpoint);
        await DbContext.SaveChangesAsync(cancellationToken);
        return new WebhookEndpointCreatedResponse(ToResponse(endpoint), secret);
    }

    public async Task DeleteAsync(Guid userId, Guid endpointId, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var endpoint = await DbContext.WebhookEndpoints.SingleOrDefaultAsync(x => x.Id == endpointId, cancellationToken);
        if (endpoint is null) return;
        endpoint.IsActive = false;
        endpoint.UpdatedAt = DateTimeOffset.UtcNow;
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task QueueEventAsync(Guid tenantId, string eventType, object payload, CancellationToken cancellationToken)
    {
        var webhookEvent = new WebhookEvent { Id = Guid.NewGuid(), TenantId = tenantId, EventType = eventType, PayloadJson = JsonSerializer.Serialize(payload), CreatedAt = DateTimeOffset.UtcNow };
        DbContext.WebhookEvents.Add(webhookEvent);
        var endpoints = await DbContext.WebhookEndpoints.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.IsActive).ToListAsync(cancellationToken);
        foreach (var endpoint in endpoints.Where(x => JsonSerializer.Deserialize<string[]>(x.EventTypesJson)?.Contains(eventType, StringComparer.OrdinalIgnoreCase) == true))
        {
            DbContext.WebhookDeliveries.Add(new WebhookDelivery
            {
                Id = Guid.NewGuid(), WebhookEndpointId = endpoint.Id, WebhookEventId = webhookEvent.Id,
                Status = "Pending", CreatedAt = DateTimeOffset.UtcNow, NextAttemptAt = DateTimeOffset.UtcNow
            });
        }
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    private static WebhookEndpointResponse ToResponse(WebhookEndpoint endpoint) =>
        new(endpoint.Id, endpoint.Url, JsonSerializer.Deserialize<string[]>(endpoint.EventTypesJson) ?? [], endpoint.IsActive, endpoint.ConsecutiveFailureCount, endpoint.CreatedAt);

}

internal static class WebhookUrlValidator
{
    public static async Task EnsurePublicHttpsAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
            throw new InvalidOperationException("Webhook URL must be a public HTTPS address without embedded credentials.");
        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Webhook URL cannot target a private network.");
        var addresses = await System.Net.Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
        if (addresses.Length == 0 || addresses.Any(IsPrivate))
            throw new InvalidOperationException("Webhook URL cannot target a private network.");
    }

    private static bool IsPrivate(System.Net.IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (System.Net.IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast) return true;
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;
        var bytes = address.GetAddressBytes();
        return bytes[0] == 10 || bytes[0] == 127 || bytes[0] == 0 || bytes[0] >= 224
            || (bytes[0] == 169 && bytes[1] == 254) || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
            || (bytes[0] == 192 && bytes[1] == 168) || (bytes[0] == 100 && bytes[1] is >= 64 and <= 127);
    }
}
