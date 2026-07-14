using OneCompetitions.Contracts.Branding;

namespace OneCompetitions.Application.Branding;

/// <summary>
/// Manages tenant brand profiles and public theme resolution.
/// </summary>
public interface IBrandProfileService
{
    Task<IReadOnlyList<BrandProfileResponse>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<BrandProfileResponse> CreateAsync(Guid userId, UpsertBrandProfileRequest request, CancellationToken cancellationToken);
    Task<BrandProfileResponse?> GetAsync(Guid userId, Guid brandId, CancellationToken cancellationToken);
    Task<BrandProfileResponse> UpdateAsync(Guid userId, Guid brandId, UpsertBrandProfileRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid brandId, CancellationToken cancellationToken);
    Task<PublicThemeResponse?> GetPublicThemeAsync(CancellationToken cancellationToken);
}
