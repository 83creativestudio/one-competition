using Microsoft.EntityFrameworkCore;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Domain.Tenants;
using OneCompetitions.Infrastructure.Persistence;
using Serilog.Context;

namespace OneCompetitions.Api.Middleware;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> LocalHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "127.0.0.1",
        "::1"
    };

    public async Task InvokeAsync(
        HttpContext context,
        AppDbContext dbContext,
        ITenantContextSetter tenantSetter,
        IConfiguration configuration)
    {
        var hostname = context.Request.Host.Host.ToLowerInvariant();
        var baseDomain = (configuration["PLATFORM_BASE_DOMAIN"] ?? "competitions.local").ToLowerInvariant();

        var tenant = await ResolveFromPathAsync(context, dbContext)
            ?? await ResolveFromPlatformSubdomainAsync(hostname, baseDomain, dbContext)
            ?? await ResolveFromAuthenticatedUserAsync(context, dbContext)
            ?? await ResolveFromCustomDomainAsync(hostname, dbContext);

        if (tenant is null)
        {
            tenantSetter.Clear(hostname);
            if (ShouldRejectUnknownHost(context, hostname, baseDomain))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(new { error = "Unknown or inactive tenant domain." });
                return;
            }

            await next(context);
            return;
        }

        if (tenant.Status is TenantStatus.Suspended or TenantStatus.Cancelled or TenantStatus.Archived)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant is not active." });
            return;
        }

        tenantSetter.Set(tenant.Id, tenant.Slug, hostname);
        using (LogContext.PushProperty("TenantId", tenant.Id))
        {
            await next(context);
        }
    }

    private static async Task<Tenant?> ResolveFromPathAsync(HttpContext context, AppDbContext dbContext)
    {
        var segments = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (segments.Length < 2 || !string.Equals(segments[0], "c", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var slug = segments[1].ToLowerInvariant();
        return await dbContext.Tenants.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Slug == slug);
    }

    private static async Task<Tenant?> ResolveFromPlatformSubdomainAsync(string hostname, string baseDomain, AppDbContext dbContext)
    {
        if (!hostname.EndsWith($".{baseDomain}", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var slug = hostname[..^($".{baseDomain}".Length)];
        if (string.IsNullOrWhiteSpace(slug) || slug.Contains('.'))
        {
            return null;
        }

        return await dbContext.Tenants.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Slug == slug);
    }

    private static async Task<Tenant?> ResolveFromAuthenticatedUserAsync(HttpContext context, AppDbContext dbContext)
    {
        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return null;
        }

        return await dbContext.Tenants.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == tenantId);
    }

    private static async Task<Tenant?> ResolveFromCustomDomainAsync(string hostname, AppDbContext dbContext)
    {
        var domain = await dbContext.TenantDomains
            .IgnoreQueryFilters()
            .Include(x => x.Tenant)
            .SingleOrDefaultAsync(x => x.Hostname == hostname && x.Status == TenantDomainStatus.Active && x.DeletedAt == null);

        return domain?.Tenant;
    }

    private static bool ShouldRejectUnknownHost(HttpContext context, string hostname, string baseDomain)
    {
        if (context.Request.Path.StartsWithSegments("/health") || context.Request.Path.StartsWithSegments("/swagger"))
        {
            return false;
        }

        return !LocalHosts.Contains(hostname) && !string.Equals(hostname, baseDomain, StringComparison.OrdinalIgnoreCase);
    }
}
