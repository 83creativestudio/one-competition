using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using OneCompetitions.Application.Auditing;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Notifications;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Contracts.Tenants;
using OneCompetitions.Domain.Tenants;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class TenantAccessService(
    AppDbContext dbContext,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    INotificationQueue notifications,
    IAuditLogger auditLogger) : ITenantAccessService
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

    public async Task<TenantResponse> UpdateCurrentTenantAsync(Guid userId, UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var tenant = await dbContext.Tenants.SingleAsync(x => x.Id == tenantContext.TenantId, cancellationToken);
        tenant.Name = request.Name.Trim();
        tenant.LegalName = request.LegalName.Trim();
        tenant.DefaultLanguage = request.DefaultLanguage.Trim().ToLowerInvariant();
        tenant.TimeZone = request.TimeZone.Trim();
        tenant.CountryCode = request.CountryCode.Trim().ToUpperInvariant();
        tenant.Currency = request.Currency.Trim().ToUpperInvariant();
        tenant.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(tenant.Id, userId, "User", "tenant.updated", "Tenant", tenant.Id.ToString(), null, Guid.NewGuid().ToString("N")), cancellationToken);
        return ToResponse(tenant);
    }

    public async Task<TenantUserResponse> InviteUserAsync(Guid userId, InviteTenantUserRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        if (!Enum.TryParse<TenantRole>(request.Role, true, out var role)) throw new InvalidOperationException("Tenant role is invalid.");
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(), UserName = email, Email = email, EmailConfirmed = false, DisplayName = email,
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
            };
            var created = await userManager.CreateAsync(user);
            if (!created.Succeeded) throw new InvalidOperationException(string.Join("; ", created.Errors.Select(x => x.Description)));
        }

        var membership = await dbContext.TenantUsers.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.TenantId == tenantContext.TenantId && x.UserId == user.Id, cancellationToken);
        if (membership is not null && membership.Status == TenantUserStatus.Active)
            throw new InvalidOperationException("This user is already an active tenant member.");

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var now = DateTimeOffset.UtcNow;
        membership ??= new TenantUser { Id = Guid.NewGuid(), TenantId = tenantContext.TenantId, UserId = user.Id, CreatedAt = now };
        if (dbContext.Entry(membership).State == EntityState.Detached) dbContext.TenantUsers.Add(membership);
        membership.Role = role;
        membership.Status = TenantUserStatus.Invited;
        membership.InvitedByUserId = userId;
        membership.InvitedAt = now;
        membership.InvitationTokenHash = AuthService.Hash(token);
        membership.InvitationExpiresAt = now.AddDays(7);
        membership.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        await notifications.QueueEmailAsync(tenantContext.TenantId, null,
            new EmailMessage(email, "You are invited to ONE. Competitions", $"Invitation ID: {membership.Id}\nOne-time token: {token}\nThis invitation expires in 7 days."), cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(tenantContext.TenantId, userId, "User", "tenant.user_invited", "TenantUser", membership.Id.ToString(), null, Guid.NewGuid().ToString("N")), cancellationToken);
        return ToResponse(membership, user);
    }

    public async Task<TenantUserResponse> UpdateUserAsync(Guid userId, Guid membershipId, UpdateTenantUserRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var membership = await dbContext.TenantUsers.SingleOrDefaultAsync(x => x.Id == membershipId, cancellationToken)
            ?? throw new InvalidOperationException("Tenant user was not found.");
        if (!Enum.TryParse<TenantRole>(request.Role, true, out var role) || !Enum.TryParse<TenantUserStatus>(request.Status, true, out var status))
            throw new InvalidOperationException("Tenant role or status is invalid.");
        await ProtectLastOwnerAsync(membership, role, status, cancellationToken);
        membership.Role = role;
        membership.Status = status;
        membership.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        var memberUser = await userManager.FindByIdAsync(membership.UserId.ToString()) ?? throw new InvalidOperationException("User was not found.");
        await auditLogger.RecordAsync(new AuditRecord(tenantContext.TenantId, userId, "User", "tenant.user_updated", "TenantUser", membership.Id.ToString(), null, Guid.NewGuid().ToString("N")), cancellationToken);
        return ToResponse(membership, memberUser);
    }

    public async Task RemoveUserAsync(Guid userId, Guid membershipId, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var membership = await dbContext.TenantUsers.SingleOrDefaultAsync(x => x.Id == membershipId, cancellationToken)
            ?? throw new InvalidOperationException("Tenant user was not found.");
        await ProtectLastOwnerAsync(membership, TenantRole.Viewer, TenantUserStatus.Removed, cancellationToken);
        membership.Status = TenantUserStatus.Removed;
        membership.UpdatedAt = DateTimeOffset.UtcNow;
        var sessions = await dbContext.UserSessions.Where(x => x.UserId == membership.UserId && x.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var session in sessions) session.RevokedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(tenantContext.TenantId, userId, "User", "tenant.user_removed", "TenantUser", membership.Id.ToString(), null, Guid.NewGuid().ToString("N")), cancellationToken);
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

    private async Task EnsureTenantManagerAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved) throw new UnauthorizedAccessException("Tenant context is required.");
        if (await UserHasPlatformRoleAsync(userId)) return;
        var allowed = await dbContext.TenantUsers.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantContext.TenantId && x.UserId == userId
            && x.Status == TenantUserStatus.Active && (x.Role == TenantRole.TenantOwner || x.Role == TenantRole.TenantAdministrator), cancellationToken);
        if (!allowed) throw new UnauthorizedAccessException("Tenant administrator access is required.");
    }

    private async Task ProtectLastOwnerAsync(TenantUser membership, TenantRole newRole, TenantUserStatus newStatus, CancellationToken cancellationToken)
    {
        if (membership.Role != TenantRole.TenantOwner || (newRole == TenantRole.TenantOwner && newStatus == TenantUserStatus.Active)) return;
        var owners = await dbContext.TenantUsers.CountAsync(x => x.TenantId == tenantContext.TenantId && x.Role == TenantRole.TenantOwner && x.Status == TenantUserStatus.Active, cancellationToken);
        if (owners <= 1) throw new InvalidOperationException("The final active tenant owner cannot be removed or demoted.");
    }

    private static TenantResponse ToResponse(Tenant x) => new(x.Id, x.Name, x.LegalName, x.Slug, x.Status.ToString(), x.DefaultLanguage, x.TimeZone, x.CountryCode, x.Currency);
    private static TenantUserResponse ToResponse(TenantUser membership, ApplicationUser user) =>
        new(membership.Id, membership.UserId, user.Email ?? string.Empty, membership.Role.ToString(), membership.Status.ToString(), membership.CreatedAt);
}
