using OneCompetitions.Contracts.Domains;

namespace OneCompetitions.Application.Domains;

/// <summary>
/// Manages tenant domains through explicit tenant authorization.
/// </summary>
public interface ITenantDomainService
{
    /// <summary>Returns the configured shared platform address without creating a tenant domain binding.</summary>
    Task<PlatformAddressResponse> GetPlatformAddressAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TenantDomainResponse>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<TenantDomainResponse> CreateAsync(Guid userId, CreateTenantDomainRequest request, CancellationToken cancellationToken);
    Task<DomainVerificationResponse> VerifyAsync(Guid userId, Guid domainId, CancellationToken cancellationToken);
    Task<TenantDomainResponse> SetPrimaryAsync(Guid userId, Guid domainId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid domainId, CancellationToken cancellationToken);
}
