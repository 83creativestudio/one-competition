using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OneCompetitions.Application.Auditing;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Notifications;
using OneCompetitions.Contracts.Auth;
using OneCompetitions.Domain.Tenants;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class AccountManagementService(AppDbContext db, UserManager<ApplicationUser> users,
    RoleManager<IdentityRole<Guid>> roles, INotificationQueue notifications, IAuditLogger audit,
    IConfiguration configuration) : IAccountManagementService
{
    public async Task RegisterAsync(RegisterCustomerRequest request, CancellationToken cancellationToken)
    {
        // The public contract contains no role or tenant ID; every registration receives a new workspace.
        if (await users.FindByEmailAsync(request.Email.Trim()) is not null) return;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var user = await ProvisionAsync(request.DisplayName, request.Email, request.Password,
            request.OrganisationName, "Customer", false, cancellationToken);
        var membership = await db.TenantUsers.IgnoreQueryFilters().SingleAsync(x => x.UserId == user.Id, cancellationToken);
        await QueueVerificationAsync(user, membership.TenantId, cancellationToken);
        await RecordAsync(user.Id, user.Id, "account.registered", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RequestVerificationAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) throw new InvalidOperationException("Email is required.");
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || user.EmailConfirmed || !user.IsActive || user.DeletedAt is not null) return;
        var tenantId = await db.TenantUsers.IgnoreQueryFilters().Where(x => x.UserId == user.Id)
            .Select(x => x.TenantId).FirstOrDefaultAsync(cancellationToken);
        await QueueVerificationAsync(user, tenantId, cancellationToken);
    }

    private async Task QueueVerificationAsync(ApplicationUser user, Guid tenantId, CancellationToken cancellationToken)
    {
        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        var origin = configuration["PUBLIC_APP_URL"] ?? $"https://{configuration["PLATFORM_BASE_DOMAIN"] ?? "competitions.local"}";
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var publicUri) || publicUri.Scheme is not ("https" or "http"))
            throw new InvalidOperationException("PUBLIC_APP_URL must be an absolute HTTP or HTTPS URL.");
        var link = $"{publicUri.GetLeftPart(UriPartial.Authority)}/account/verify-email?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";
        await notifications.QueueEmailAsync(tenantId, null,
            new EmailMessage(user.Email!, "Verify your ONE. Competitions account", $"Confirm your email address to sign in:\n\n{link}"), cancellationToken);
    }

    public async Task<AccountPageResponse> ListAsync(Guid actorId, string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        await EnsureAdminAsync(actorId);
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Users.AsNoTracking().Where(x => x.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(x => x.NormalizedEmail!.Contains(term) || x.DisplayName.ToUpper().Contains(term));
        }
        var total = await query.CountAsync(cancellationToken);
        var accounts = await query.OrderBy(x => x.NormalizedEmail).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var result = new List<AccountResponse>();
        foreach (var user in accounts) result.Add(await ResponseAsync(user, cancellationToken));
        return new AccountPageResponse(result, total, page, pageSize);
    }

    public async Task<AccountResponse> CreateAsync(Guid actorId, CreateAccountRequest request, CancellationToken cancellationToken)
    {
        await EnsureAdminAsync(actorId);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var user = await ProvisionAsync(request.DisplayName, request.Email, request.Password,
            request.OrganisationName, request.Role, true, cancellationToken);
        await RecordAsync(actorId, user.Id, "account.created", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await ResponseAsync(user, cancellationToken);
    }

    public async Task<AccountResponse> UpdateAsync(Guid actorId, Guid userId, UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        await EnsureAdminAsync(actorId);
        ValidateRole(request.Role);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var user = await FindAsync(userId, cancellationToken);
        await ProtectAdminAsync(actorId, user, request.Role == "Admin" && request.IsActive, cancellationToken);
        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null && existing.Id != userId) throw new InvalidOperationException("This email address is already in use.");
        user.DisplayName = request.DisplayName.Trim();
        user.Email = email;
        user.UserName = email;
        user.IsActive = request.IsActive;
        user.IsPlatformUser = request.Role == "Admin";
        user.UpdatedAt = DateTimeOffset.UtcNow;
        Check(await users.UpdateAsync(user));
        if (request.NewPassword is not null)
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            Check(await users.ResetPasswordAsync(user, token, request.NewPassword));
            await RecordAsync(actorId, userId, "account.password.reset", cancellationToken);
        }
        if (request.UnlockLogin)
        {
            Check(await users.SetLockoutEndDateAsync(user, null));
            Check(await users.ResetAccessFailedCountAsync(user));
            await RecordAsync(actorId, userId, "account.login.unlocked", cancellationToken);
        }
        var currentRoles = await users.GetRolesAsync(user);
        if (request.Role == "Customer" && currentRoles.Count > 0) Check(await users.RemoveFromRolesAsync(user, currentRoles));
        if (request.Role == "Admin" && !currentRoles.Contains(AppRoles.PlatformOwner) && !currentRoles.Contains(AppRoles.PlatformAdministrator))
        {
            await EnsureAdminRoleAsync();
            Check(await users.AddToRoleAsync(user, AppRoles.PlatformAdministrator));
        }
        await RevokeAsync(user, cancellationToken);
        await RecordAsync(actorId, userId, "account.updated", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await ResponseAsync(user, cancellationToken);
    }

    public async Task DeleteAsync(Guid actorId, Guid userId, CancellationToken cancellationToken)
    {
        await EnsureAdminAsync(actorId);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var user = await FindAsync(userId, cancellationToken);
        if (actorId == userId) throw new InvalidOperationException("You cannot delete your own account.");
        await ProtectAdminAsync(actorId, user, false, cancellationToken);
        user.IsActive = false;
        user.DeletedAt = DateTimeOffset.UtcNow;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        Check(await users.UpdateAsync(user));
        var memberships = await db.TenantUsers.IgnoreQueryFilters().Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        foreach (var membership in memberships) { membership.Status = TenantUserStatus.Removed; membership.UpdatedAt = DateTimeOffset.UtcNow; }
        await RevokeAsync(user, cancellationToken);
        await RecordAsync(actorId, userId, "account.deleted", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<ApplicationUser> ProvisionAsync(string displayName, string email, string password,
        string organisation, string role, bool confirmed, CancellationToken cancellationToken)
    {
        ValidateRole(role);
        email = email.Trim().ToLowerInvariant();
        if (await users.FindByEmailAsync(email) is not null) throw new InvalidOperationException("This email address is already in use.");
        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(organisation))
            throw new InvalidOperationException("Name and organisation are required.");
        var now = DateTimeOffset.UtcNow;
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email, Email = email,
            DisplayName = displayName.Trim(), EmailConfirmed = confirmed, IsPlatformUser = role == "Admin",
            LockoutEnabled = true, CreatedAt = now, UpdatedAt = now };
        Check(await users.CreateAsync(user, password));
        if (role == "Admin")
        {
            await EnsureAdminRoleAsync();
            Check(await users.AddToRoleAsync(user, AppRoles.PlatformAdministrator));
        }
        var tenantId = Guid.NewGuid();
        var slug = $"workspace-{tenantId:N}";
        db.Tenants.Add(new Tenant { Id = tenantId, Name = organisation.Trim(), LegalName = organisation.Trim(),
            Slug = slug, Status = TenantStatus.Active, CreatedAt = now, UpdatedAt = now });
        db.TenantUsers.Add(new TenantUser { Id = Guid.NewGuid(), TenantId = tenantId, UserId = user.Id,
            Role = TenantRole.TenantOwner, Status = TenantUserStatus.Active, AcceptedAt = now, CreatedAt = now, UpdatedAt = now });
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    private async Task EnsureAdminAsync(Guid actorId)
    {
        var actor = await users.FindByIdAsync(actorId.ToString());
        if (actor is null || !actor.IsActive || actor.DeletedAt is not null || !await IsAdminAsync(actor))
            throw new UnauthorizedAccessException("Platform administrator access is required.");
    }

    private async Task ProtectAdminAsync(Guid actorId, ApplicationUser user, bool remainsAdmin, CancellationToken cancellationToken)
    {
        if (remainsAdmin) return;
        if (actorId == user.Id) throw new InvalidOperationException("You cannot remove your own administrator access.");
        if (!await IsAdminAsync(user)) return;
        var adminRoleIds = db.Roles.Where(x => x.Name == AppRoles.PlatformAdministrator || x.Name == AppRoles.PlatformOwner).Select(x => x.Id);
        var otherAdminIds = db.UserRoles.Where(x => adminRoleIds.Contains(x.RoleId) && x.UserId != user.Id).Select(x => x.UserId);
        if (!await db.Users.AnyAsync(x => otherAdminIds.Contains(x.Id) && x.IsActive && x.DeletedAt == null && x.EmailConfirmed, cancellationToken))
            throw new InvalidOperationException("The final active administrator cannot be removed or demoted.");
    }

    private async Task RevokeAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        Check(await users.UpdateSecurityStampAsync(user));
        var sessions = await db.UserSessions.Where(x => x.UserId == user.Id && x.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var session in sessions) session.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApplicationUser> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Users.SingleOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");

    private async Task<bool> IsAdminAsync(ApplicationUser user) =>
        await users.IsInRoleAsync(user, AppRoles.PlatformOwner) || await users.IsInRoleAsync(user, AppRoles.PlatformAdministrator);

    private async Task EnsureAdminRoleAsync()
    {
        if (!await roles.RoleExistsAsync(AppRoles.PlatformAdministrator))
            Check(await roles.CreateAsync(new IdentityRole<Guid>(AppRoles.PlatformAdministrator)));
    }

    private async Task<AccountResponse> ResponseAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var workspaces = await db.TenantUsers.IgnoreQueryFilters().Where(x => x.UserId == user.Id && x.Status == TenantUserStatus.Active)
            .Join(db.Tenants.IgnoreQueryFilters(), x => x.TenantId, x => x.Id, (membership, tenant) => new AccountWorkspaceResponse(tenant.Id, tenant.Name, tenant.Slug))
            .ToListAsync(cancellationToken);
        return new AccountResponse(user.Id, user.DisplayName, user.Email ?? "", await IsAdminAsync(user) ? "Admin" : "Customer",
            user.IsActive, user.EmailConfirmed, user.TwoFactorEnabled, user.CreatedAt, workspaces, await users.IsLockedOutAsync(user));
    }

    private Task RecordAsync(Guid actorId, Guid userId, string action, CancellationToken cancellationToken) =>
        audit.RecordAsync(new AuditRecord(Guid.Empty, actorId, "User", action, "User", userId.ToString(), null, Guid.NewGuid().ToString("N")), cancellationToken);

    private static void ValidateRole(string role)
    {
        if (role is not ("Admin" or "Customer")) throw new InvalidOperationException("Select Admin or Customer.");
    }

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }
}
