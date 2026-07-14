using System.ComponentModel.DataAnnotations;

namespace OneCompetitions.Contracts.Branding;

public sealed record UpsertBrandProfileRequest(
    [Required] string Name,
    [Required] string PrimaryColor,
    [Required] string SecondaryColor,
    [Required] string AccentColor,
    [Required] string BackgroundColor,
    [Required] string TextColor,
    string HeadingFont,
    string BodyFont,
    string ButtonStyle,
    int BorderRadius,
    string? FooterText,
    string? SupportEmail,
    string? SupportPhone,
    bool ShowPoweredBy,
    string? CustomCss);
