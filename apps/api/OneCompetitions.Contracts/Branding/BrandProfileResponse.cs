namespace OneCompetitions.Contracts.Branding;

public sealed record BrandProfileResponse(
    Guid Id,
    Guid TenantId,
    string Name,
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
    string? SupportEmail,
    string? SupportPhone,
    bool ShowPoweredBy,
    string? CustomCss,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
