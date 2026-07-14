using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Domain.Tenants;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public abstract class TenantScopedServiceBase(
    AppDbContext dbContext,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager)
{
    protected AppDbContext DbContext { get; } = dbContext;
    protected ITenantContext TenantContext { get; } = tenantContext;

    protected async Task EnsureTenantMemberAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!TenantContext.IsResolved)
        {
            throw new UnauthorizedAccessException("Tenant context is required.");
        }

        if (await IsPlatformAdminAsync(userId))
        {
            return;
        }

        var allowed = await DbContext.TenantUsers
            .IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == TenantContext.TenantId
                && x.UserId == userId
                && x.Status == TenantUserStatus.Active,
                cancellationToken);

        if (!allowed)
        {
            throw new UnauthorizedAccessException("Tenant access is required.");
        }
    }

    protected async Task EnsureTenantManagerAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!TenantContext.IsResolved)
        {
            throw new UnauthorizedAccessException("Tenant context is required.");
        }

        if (await IsPlatformAdminAsync(userId))
        {
            return;
        }

        var allowed = await DbContext.TenantUsers
            .IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == TenantContext.TenantId
                && x.UserId == userId
                && x.Status == TenantUserStatus.Active
                && (x.Role == TenantRole.TenantOwner
                    || x.Role == TenantRole.TenantAdministrator
                    || x.Role == TenantRole.CompetitionManager
                    || x.Role == TenantRole.DrawOperator
                    || x.Role == TenantRole.EntryReviewer),
                cancellationToken);

        if (!allowed)
        {
            throw new UnauthorizedAccessException("Tenant manager access is required.");
        }
    }

    protected async Task<bool> IsPlatformAdminAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is not null
            && (await userManager.IsInRoleAsync(user, AppRoles.PlatformOwner)
                || await userManager.IsInRoleAsync(user, AppRoles.PlatformAdministrator));
    }
}
