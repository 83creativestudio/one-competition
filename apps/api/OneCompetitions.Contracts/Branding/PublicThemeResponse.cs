namespace OneCompetitions.Contracts.Branding;

public sealed record PublicThemeResponse(
    Guid TenantId,
    string TenantSlug,
    string TenantName,
    string BrandName,
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string BackgroundColor,
    string TextColor,
    string HeadingFont,
    string BodyFont,
    string ButtonStyle,
    int BorderRadius,
    string? FooterText,
    bool ShowPoweredBy);
