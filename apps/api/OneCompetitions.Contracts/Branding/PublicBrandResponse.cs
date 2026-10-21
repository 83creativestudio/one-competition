namespace OneCompetitions.Contracts.Branding;

public sealed record PublicBrandIdentityResponse(Guid Id, string Name, Guid? LogoAssetId,
    string PrimaryColor, string BackgroundColor, string TextColor);
public sealed record BrandCompetitionResponse(string Name, string Slug, string? Description,
    string Status, DateTimeOffset StartsAt, DateTimeOffset EndsAt, Guid? CoverAssetId);
public sealed record PublicBrandResponse(string TenantSlug, PublicBrandIdentityResponse Brand,
    string? FooterText, IReadOnlyList<BrandCompetitionResponse> Competitions, int Total, int Page, int PageSize);
