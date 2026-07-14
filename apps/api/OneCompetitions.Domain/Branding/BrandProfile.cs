using OneCompetitions.Domain.Common;
using OneCompetitions.Domain.Tenants;

namespace OneCompetitions.Domain.Branding;

public sealed class BrandProfile : TenantScopedEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public Guid? LogoAssetId { get; set; }
    public Guid? DarkLogoAssetId { get; set; }
    public Guid? FaviconAssetId { get; set; }
    public string PrimaryColor { get; set; } = "#0f766e";
    public string SecondaryColor { get; set; } = "#111827";
    public string AccentColor { get; set; } = "#c2410c";
    public string BackgroundColor { get; set; } = "#ffffff";
    public string TextColor { get; set; } = "#111827";
    public string HeadingFont { get; set; } = "Inter";
    public string BodyFont { get; set; } = "Inter";
    public string ButtonStyle { get; set; } = "Solid";
    public int BorderRadius { get; set; } = 6;
    public Guid? EmailHeaderAssetId { get; set; }
    public Guid? SocialShareAssetId { get; set; }
    public string? FooterText { get; set; }
    public string? SupportEmail { get; set; }
    public string? SupportPhone { get; set; }
    public bool ShowPoweredBy { get; set; } = true;
    public string? CustomCss { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Tenant? Tenant { get; set; }
}
