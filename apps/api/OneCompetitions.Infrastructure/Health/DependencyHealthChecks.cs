using Microsoft.Extensions.Diagnostics.HealthChecks;
using OneCompetitions.Application.Storage;
using OneCompetitions.Infrastructure.Persistence;
using StackExchange.Redis;

namespace OneCompetitions.Infrastructure.Health;

public sealed class DatabaseHealthCheck(AppDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await dbContext.Database.CanConnectAsync(cancellationToken) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Database connection failed.");
}

public sealed class ObjectStorageHealthCheck(IObjectStorage storage) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        const string key = "system/health/readiness.txt";
        try
        {
            await using var stream = new MemoryStream("ok"u8.ToArray());
            await storage.PutAsync(key, stream, "text/plain", cancellationToken);
            if (!await storage.ExistsAsync(key, cancellationToken)) return HealthCheckResult.Unhealthy("Object storage read-after-write check failed.");
            await using var content = await storage.OpenReadAsync(key, cancellationToken);
            if (content.ReadByte() != 'o') return HealthCheckResult.Unhealthy("Object storage read check failed.");
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) { return HealthCheckResult.Unhealthy("Object storage connection failed.", exception); }
        finally
        {
            try { await storage.DeleteAsync(key, cancellationToken); }
            catch { /* The primary health result already describes storage availability. */ }
        }
    }
}

public sealed class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try { return await redis.GetDatabase().PingAsync() < TimeSpan.FromSeconds(2) ? HealthCheckResult.Healthy() : HealthCheckResult.Degraded("Redis latency is high."); }
        catch (Exception exception) { return HealthCheckResult.Unhealthy("Redis connection failed.", exception); }
    }
}
