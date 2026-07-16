using OneCompetitions.Contracts.Billing;
using OneCompetitions.Contracts.Platform;

namespace OneCompetitions.Application.Platform;

public interface IPlatformAdministrationService
{
    Task<PlatformTenantDetailResponse?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken);
    Task UpdateTenantStatusAsync(Guid actorUserId, Guid tenantId, UpdateTenantStatusRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlatformDomainResponse>> ListDomainsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<FeatureFlagResponse>> ListFeaturesAsync(CancellationToken cancellationToken);
    Task<FeatureFlagResponse> UpsertFeatureAsync(Guid? id, UpsertFeatureFlagRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlanResponse>> ListPlansAsync(CancellationToken cancellationToken);
    Task<PlanResponse> UpsertPlanAsync(Guid? id, UpsertPlanRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ResellerResponse>> ListResellersAsync(CancellationToken cancellationToken);
    Task<ResellerResponse> UpsertResellerAsync(Guid? id, UpsertResellerRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEventResponse>> ListPlatformAuditAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEventResponse>> ListTenantAuditAsync(Guid userId, CancellationToken cancellationToken);
}
