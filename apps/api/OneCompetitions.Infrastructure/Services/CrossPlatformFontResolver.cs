using PdfSharp.Fonts;

namespace OneCompetitions.Infrastructure.Services;

public sealed class CrossPlatformFontResolver : IFontResolver
{
    private readonly string _regular = Find(
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/Library/Fonts/Arial.ttf");
    private readonly string _bold = Find(
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
        "/Library/Fonts/Arial Bold.ttf");

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) => new(isBold ? "one-bold" : "one-regular");

    public byte[] GetFont(string faceName) => File.ReadAllBytes(faceName == "one-bold" ? _bold : _regular);

    private static string Find(params string[] paths) => paths.FirstOrDefault(File.Exists)
        ?? throw new InvalidOperationException("A supported TrueType font was not found. Install fonts-dejavu-core in the runtime image.");
}
