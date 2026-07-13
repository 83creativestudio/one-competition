using OneCompetitions.Contracts.Tenants;

namespace OneCompetitions.Application.Tenants;

/// <summary>
/// Reads tenant information through explicit authorization and tenant checks.
/// </summary>
public interface ITenantAccessService
{
    Task<TenantResponse?> GetCurrentTenantAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TenantUserResponse>> GetCurrentTenantUsersAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TenantSummaryResponse>> GetPlatformTenantsAsync(Guid userId, CancellationToken cancellationToken);
}
