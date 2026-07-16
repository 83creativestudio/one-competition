using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OneCompetitions.Application.Auditing;
using OneCompetitions.Application.Platform;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Contracts.Billing;
using OneCompetitions.Contracts.Domains;
using OneCompetitions.Contracts.Platform;
using OneCompetitions.Contracts.Tenants;
using OneCompetitions.Domain.Billing;
using OneCompetitions.Domain.Features;
using OneCompetitions.Domain.Tenants;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class PlatformAdministrationService(AppDbContext dbContext, ITenantContext tenantContext, IAuditLogger auditLogger) : IPlatformAdministrationService
{
    public async Task<PlatformTenantDetailResponse?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken);
        if (tenant is null) return null;
        var domains = await dbContext.TenantDomains.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.DeletedAt == null).OrderBy(x => x.Hostname).Select(x => DomainResponse(x)).ToListAsync(cancellationToken);
        var users = await dbContext.TenantUsers.IgnoreQueryFilters().CountAsync(x => x.TenantId == tenantId && x.Status == TenantUserStatus.Active, cancellationToken);
        var subscription = (await dbContext.TenantSubscriptions.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ToListAsync(cancellationToken)).MaxBy(x => x.CreatedAt);
        return new PlatformTenantDetailResponse(TenantResponse(tenant), users, domains, subscription is null ? null : SubscriptionResponse(subscription));
    }

    public async Task UpdateTenantStatusAsync(Guid actorUserId, Guid tenantId, UpdateTenantStatusRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("A reason is required.");
        if (!Enum.TryParse<TenantStatus>(request.Status, true, out var status)) throw new InvalidOperationException("Tenant status is invalid.");
        var tenant = await dbContext.Tenants.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken) ?? throw new InvalidOperationException("Tenant was not found.");
        tenant.Status = status; tenant.SuspendedAt = status == TenantStatus.Suspended ? DateTimeOffset.UtcNow : null; tenant.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(tenantId, actorUserId, "User", "tenant.status_updated", "Tenant", tenantId.ToString(), request.Reason, Guid.NewGuid().ToString("N")), cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformDomainResponse>> ListDomainsAsync(CancellationToken cancellationToken) =>
        await dbContext.TenantDomains.IgnoreQueryFilters().Where(x => x.DeletedAt == null).Join(dbContext.Tenants.IgnoreQueryFilters(), x => x.TenantId, x => x.Id,
            (domain, tenant) => new PlatformDomainResponse(domain.Id, tenant.Id, tenant.Name, domain.Hostname, domain.DomainType.ToString(), domain.Status.ToString(), domain.SslStatus, domain.CertificateExpiresAt))
            .OrderBy(x => x.Hostname).Take(500).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FeatureFlagResponse>> ListFeaturesAsync(CancellationToken cancellationToken) =>
        await dbContext.FeatureFlags.OrderBy(x => x.Code).Select(x => FeatureResponse(x)).ToListAsync(cancellationToken);

    public async Task<FeatureFlagResponse> UpsertFeatureAsync(Guid? id, UpsertFeatureFlagRequest request, CancellationToken cancellationToken)
    {
        var feature = id is null ? null : await dbContext.FeatureFlags.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        feature ??= new FeatureFlag { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow };
        if (dbContext.Entry(feature).State == EntityState.Detached) dbContext.FeatureFlags.Add(feature);
        feature.Code = request.Code.Trim(); feature.Name = request.Name.Trim(); feature.IsEnabled = request.IsEnabled;
        feature.Environment = request.Environment.Trim(); feature.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken); return FeatureResponse(feature);
    }

    public async Task<IReadOnlyList<PlanResponse>> ListPlansAsync(CancellationToken cancellationToken) =>
        await dbContext.Plans.OrderBy(x => x.MonthlyPrice).Select(x => PlanResponse(x)).ToListAsync(cancellationToken);

    public async Task<PlanResponse> UpsertPlanAsync(Guid? id, UpsertPlanRequest request, CancellationToken cancellationToken)
    {
        _ = JsonDocument.Parse(request.FeatureConfigurationJson);
        var plan = id is null ? null : await dbContext.Plans.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        plan ??= new Plan { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow };
        if (dbContext.Entry(plan).State == EntityState.Detached) dbContext.Plans.Add(plan);
        plan.Name = request.Name.Trim(); plan.Code = request.Code.Trim().ToLowerInvariant(); plan.MonthlyPrice = request.MonthlyPrice;
        plan.AnnualPrice = request.AnnualPrice; plan.FeatureConfigurationJson = request.FeatureConfigurationJson; plan.IsActive = request.IsActive; plan.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken); return PlanResponse(plan);
    }

    public async Task<IReadOnlyList<ResellerResponse>> ListResellersAsync(CancellationToken cancellationToken) =>
        await dbContext.Resellers.OrderBy(x => x.Name).Select(x => new ResellerResponse(x.Id, x.Name, x.Slug, x.Status, dbContext.Tenants.IgnoreQueryFilters().Count(t => t.ResellerId == x.Id))).ToListAsync(cancellationToken);

    public async Task<ResellerResponse> UpsertResellerAsync(Guid? id, UpsertResellerRequest request, CancellationToken cancellationToken)
    {
        var reseller = id is null ? null : await dbContext.Resellers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        reseller ??= new Reseller { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow };
        if (dbContext.Entry(reseller).State == EntityState.Detached) dbContext.Resellers.Add(reseller);
        reseller.Name = request.Name.Trim(); reseller.Slug = request.Slug.Trim().ToLowerInvariant(); reseller.Status = request.Status.Trim(); reseller.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new ResellerResponse(reseller.Id, reseller.Name, reseller.Slug, reseller.Status, await dbContext.Tenants.IgnoreQueryFilters().CountAsync(x => x.ResellerId == reseller.Id, cancellationToken));
    }

    public async Task<IReadOnlyList<AuditEventResponse>> ListPlatformAuditAsync(CancellationToken cancellationToken) =>
        await dbContext.AuditEvents.IgnoreQueryFilters().OrderByDescending(x => x.OccurredAt).Take(500).Select(x => AuditResponse(x)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AuditEventResponse>> ListTenantAuditAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved) throw new UnauthorizedAccessException("Tenant context is required.");
        var member = await dbContext.TenantUsers.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantContext.TenantId && x.UserId == userId && x.Status == TenantUserStatus.Active, cancellationToken);
        if (!member) throw new UnauthorizedAccessException("Tenant access is required.");
        return await dbContext.AuditEvents.OrderByDescending(x => x.OccurredAt).Take(500).Select(x => AuditResponse(x)).ToListAsync(cancellationToken);
    }

    private static TenantResponse TenantResponse(Tenant x) => new(x.Id, x.Name, x.LegalName, x.Slug, x.Status.ToString(), x.DefaultLanguage, x.TimeZone, x.CountryCode, x.Currency);
    private static TenantDomainResponse DomainResponse(TenantDomain x) => new(x.Id, x.TenantId, x.Hostname, x.DomainType.ToString(), x.Status.ToString(), x.IsPrimary, x.VerificationMethod, x.VerificationToken, x.ExpectedDnsTarget, x.VerifiedAt, x.LastCheckedAt, x.SslStatus, x.SslProvisionedAt, x.CertificateExpiresAt, x.FailureReason, x.CreatedAt, x.UpdatedAt);
    private static SubscriptionResponse SubscriptionResponse(TenantSubscription x) => new(x.Id, x.PlanId, x.Status, x.CurrentPeriodStartsAt, x.CurrentPeriodEndsAt, x.TrialEndsAt, x.CancelAtPeriodEnd);
    private static FeatureFlagResponse FeatureResponse(FeatureFlag x) => new(x.Id, x.Code, x.Name, x.IsEnabled, x.Environment);
    private static PlanResponse PlanResponse(Plan x) => new(x.Id, x.Name, x.Code, x.MonthlyPrice, x.AnnualPrice, JsonSerializer.Deserialize<Dictionary<string, object?>>(x.FeatureConfigurationJson) ?? []);
    private static AuditEventResponse AuditResponse(Domain.Auditing.AuditEvent x) => new(x.Id, x.TenantId, x.ActorUserId, x.ActorType, x.Action, x.EntityType, x.EntityId, x.OccurredAt, x.CorrelationId);
}
