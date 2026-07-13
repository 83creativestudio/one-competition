using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Contracts.Tenants;
using OneCompetitions.Domain.Tenants;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class TenantAccessService(
    AppDbContext dbContext,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager) : ITenantAccessService
{
    public async Task<TenantResponse?> GetCurrentTenantAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved || !await UserCanAccessTenantAsync(userId, tenantContext.TenantId, cancellationToken))
        {
            return null;
        }

        return await dbContext.Tenants
            .Where(x => x.Id == tenantContext.TenantId)
            .Select(x => new TenantResponse(x.Id, x.Name, x.LegalName, x.Slug, x.Status.ToString(), x.DefaultLanguage, x.TimeZone, x.CountryCode, x.Currency))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantUserResponse>> GetCurrentTenantUsersAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved || !await UserCanAccessTenantAsync(userId, tenantContext.TenantId, cancellationToken))
        {
            return [];
        }

        return await dbContext.TenantUsers
            .Where(x => x.TenantId == tenantContext.TenantId)
            .Join(dbContext.Users, x => x.UserId, x => x.Id, (tenantUser, user) => new TenantUserResponse(
                tenantUser.Id,
                tenantUser.UserId,
                user.Email ?? string.Empty,
                tenantUser.Role.ToString(),
                tenantUser.Status.ToString(),
                tenantUser.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantSummaryResponse>> GetPlatformTenantsAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!await UserHasPlatformRoleAsync(userId))
        {
            return [];
        }

        return await dbContext.Tenants
            .IgnoreQueryFilters()
            .OrderBy(x => x.Name)
            .Select(x => new TenantSummaryResponse(
                x.Id,
                x.Name,
                x.Slug,
                x.Status.ToString(),
                x.Users.Count(u => u.Status == TenantUserStatus.Active),
                x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> UserCanAccessTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        if (await UserHasPlatformRoleAsync(userId))
        {
            return true;
        }

        return await dbContext.TenantUsers
            .IgnoreQueryFilters()
            .AnyAsync(x => x.UserId == userId && x.TenantId == tenantId && x.Status == TenantUserStatus.Active, cancellationToken);
    }

    private async Task<bool> UserHasPlatformRoleAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        foreach (var role in AppRoles.PlatformRoles)
        {
            if (await userManager.IsInRoleAsync(user, role))
            {
                return true;
            }
        }

        return false;
    }
}
