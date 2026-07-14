using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneCompetitions.Application.Auditing;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Domains;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Contracts.Domains;
using OneCompetitions.Domain.Tenants;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class TenantDomainService(
    AppDbContext dbContext,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    IDomainVerificationProvider verificationProvider,
    IAuditLogger auditLogger) : ITenantDomainService
{
    private static readonly Regex HostnamePattern = new(
        @"^(?=.{1,253}$)(?!-)([a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z]{2,63}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task<IReadOnlyList<TenantDomainResponse>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);

        return await dbContext.TenantDomains
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.Hostname)
            .Select(x => ToResponse(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantDomainResponse> CreateAsync(Guid userId, CreateTenantDomainRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);

        var hostname = NormalizeHostname(request.Hostname);
        if (!HostnamePattern.IsMatch(hostname))
        {
            throw new InvalidOperationException("Hostname is not valid.");
        }

        if (!Enum.TryParse<TenantDomainType>(request.DomainType, true, out var domainType))
        {
            throw new InvalidOperationException("Domain type is not valid.");
        }

        if (domainType is TenantDomainType.PlatformPath or TenantDomainType.PlatformSubdomain)
        {
            throw new InvalidOperationException("Platform domains are system-managed.");
        }

        if (await dbContext.TenantDomains.IgnoreQueryFilters().AnyAsync(x => x.Hostname == hostname && x.DeletedAt == null, cancellationToken))
        {
            throw new InvalidOperationException("Domain is already assigned.");
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;
        var domain = new TenantDomain
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            Hostname = hostname,
            DomainType = domainType,
            Status = TenantDomainStatus.AwaitingDns,
            IsPrimary = false,
            VerificationMethod = "DnsTxt",
            VerificationToken = token,
            ExpectedDnsTarget = $"one-competitions-verify={token}",
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.TenantDomains.Add(domain);
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync(userId, "domain.created", domain.Id, cancellationToken);

        return ToResponse(domain);
    }

    public async Task<DomainVerificationResponse> VerifyAsync(Guid userId, Guid domainId, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);

        var domain = await dbContext.TenantDomains.SingleOrDefaultAsync(x => x.Id == domainId, cancellationToken)
            ?? throw new InvalidOperationException("Domain was not found.");

        if (string.IsNullOrWhiteSpace(domain.VerificationToken))
        {
            throw new InvalidOperationException("Domain does not have a verification token.");
        }

        domain.Status = TenantDomainStatus.Verifying;
        domain.LastCheckedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var result = await verificationProvider.VerifyAsync(domain.Hostname, domain.VerificationToken, cancellationToken);
        domain.LastCheckedAt = DateTimeOffset.UtcNow;
        domain.ExpectedDnsTarget = result.ExpectedDnsTarget ?? domain.ExpectedDnsTarget;
        domain.FailureReason = result.FailureReason;

        if (result.IsVerified)
        {
            domain.Status = TenantDomainStatus.Active;
            domain.VerifiedAt = DateTimeOffset.UtcNow;
            domain.FailureReason = null;
        }
        else
        {
            domain.Status = TenantDomainStatus.DnsError;
        }

        domain.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync(userId, "domain.verified", domain.Id, cancellationToken);

        return new DomainVerificationResponse(
            domain.Id,
            domain.Hostname,
            domain.Status.ToString(),
            result.IsVerified,
            domain.FailureReason,
            domain.LastCheckedAt ?? DateTimeOffset.UtcNow);
    }

    public async Task<TenantDomainResponse> SetPrimaryAsync(Guid userId, Guid domainId, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);

        var domain = await dbContext.TenantDomains.SingleOrDefaultAsync(x => x.Id == domainId, cancellationToken)
            ?? throw new InvalidOperationException("Domain was not found.");

        if (domain.Status != TenantDomainStatus.Active)
        {
            throw new InvalidOperationException("Only active domains can be primary.");
        }

        var domains = await dbContext.TenantDomains.ToListAsync(cancellationToken);
        foreach (var tenantDomain in domains)
        {
            tenantDomain.IsPrimary = tenantDomain.Id == domain.Id;
            tenantDomain.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync(userId, "domain.primary_set", domain.Id, cancellationToken);

        return ToResponse(domain);
    }

    public async Task DeleteAsync(Guid userId, Guid domainId, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);

        var domain = await dbContext.TenantDomains.SingleOrDefaultAsync(x => x.Id == domainId, cancellationToken)
            ?? throw new InvalidOperationException("Domain was not found.");

        if (domain.DomainType is TenantDomainType.PlatformPath or TenantDomainType.PlatformSubdomain)
        {
            throw new InvalidOperationException("Platform domains cannot be deleted from the tenant dashboard.");
        }

        domain.DeletedAt = DateTimeOffset.UtcNow;
        domain.Status = TenantDomainStatus.Disconnected;
        domain.IsPrimary = false;
        domain.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync(userId, "domain.deleted", domain.Id, cancellationToken);
    }

    private async Task EnsureTenantManagerAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
        {
            throw new UnauthorizedAccessException("Tenant context is required.");
        }

        if (await IsPlatformAdminAsync(userId))
        {
            return;
        }

        var allowed = await dbContext.TenantUsers
            .IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == tenantContext.TenantId
                && x.UserId == userId
                && x.Status == TenantUserStatus.Active
                && (x.Role == TenantRole.TenantOwner || x.Role == TenantRole.TenantAdministrator),
                cancellationToken);

        if (!allowed)
        {
            throw new UnauthorizedAccessException("Tenant administrator access is required.");
        }
    }

    private async Task<bool> IsPlatformAdminAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is not null && (await userManager.IsInRoleAsync(user, AppRoles.PlatformOwner) || await userManager.IsInRoleAsync(user, AppRoles.PlatformAdministrator));
    }

    private async Task RecordAuditAsync(Guid userId, string action, Guid entityId, CancellationToken cancellationToken)
    {
        await auditLogger.RecordAsync(new AuditRecord(tenantContext.TenantId, userId, "User", action, "TenantDomain", entityId.ToString(), null, Guid.NewGuid().ToString("N")), cancellationToken);
    }

    private static string NormalizeHostname(string hostname)
    {
        return hostname.Trim().TrimEnd('.').ToLowerInvariant();
    }

    private static TenantDomainResponse ToResponse(TenantDomain domain)
    {
        return new TenantDomainResponse(
            domain.Id,
            domain.TenantId,
            domain.Hostname,
            domain.DomainType.ToString(),
            domain.Status.ToString(),
            domain.IsPrimary,
            domain.VerificationMethod,
            domain.VerificationToken,
            domain.ExpectedDnsTarget,
            domain.VerifiedAt,
            domain.LastCheckedAt,
            domain.FailureReason,
            domain.CreatedAt,
            domain.UpdatedAt);
    }
}
