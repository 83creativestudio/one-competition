using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneCompetitions.Application.Auditing;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Branding;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Contracts.Branding;
using OneCompetitions.Domain.Branding;
using OneCompetitions.Domain.Tenants;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class BrandProfileService(
    AppDbContext dbContext,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    IAuditLogger auditLogger) : IBrandProfileService
{
    private static readonly Regex HexColorPattern = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);
    private static readonly Regex CssScriptPattern = new(@"</?\s*script|javascript:|expression\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task<IReadOnlyList<BrandProfileResponse>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);

        return await dbContext.BrandProfiles
            .OrderBy(x => x.Name)
            .Select(x => ToResponse(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<BrandProfileResponse> CreateAsync(Guid userId, UpsertBrandProfileRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        Validate(request);

        var now = DateTimeOffset.UtcNow;
        var profile = new BrandProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            Name = request.Name.Trim(),
            PrimaryColor = request.PrimaryColor,
            SecondaryColor = request.SecondaryColor,
            AccentColor = request.AccentColor,
            BackgroundColor = request.BackgroundColor,
            TextColor = request.TextColor,
            HeadingFont = DefaultIfBlank(request.HeadingFont, "Inter"),
            BodyFont = DefaultIfBlank(request.BodyFont, "Inter"),
            ButtonStyle = DefaultIfBlank(request.ButtonStyle, "Solid"),
            BorderRadius = request.BorderRadius,
            FooterText = request.FooterText,
            SupportEmail = request.SupportEmail,
            SupportPhone = request.SupportPhone,
            ShowPoweredBy = request.ShowPoweredBy,
            CustomCss = SanitizeCss(request.CustomCss),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.BrandProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync(userId, "brand.created", profile.Id, cancellationToken);

        return ToResponse(profile);
    }

    public async Task<BrandProfileResponse?> GetAsync(Guid userId, Guid brandId, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);

        return await dbContext.BrandProfiles
            .Where(x => x.Id == brandId)
            .Select(x => ToResponse(x))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<BrandProfileResponse> UpdateAsync(Guid userId, Guid brandId, UpsertBrandProfileRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        Validate(request);

        var profile = await dbContext.BrandProfiles.SingleOrDefaultAsync(x => x.Id == brandId, cancellationToken)
            ?? throw new InvalidOperationException("Brand profile was not found.");

        profile.Name = request.Name.Trim();
        profile.PrimaryColor = request.PrimaryColor;
        profile.SecondaryColor = request.SecondaryColor;
        profile.AccentColor = request.AccentColor;
        profile.BackgroundColor = request.BackgroundColor;
        profile.TextColor = request.TextColor;
        profile.HeadingFont = DefaultIfBlank(request.HeadingFont, "Inter");
        profile.BodyFont = DefaultIfBlank(request.BodyFont, "Inter");
        profile.ButtonStyle = DefaultIfBlank(request.ButtonStyle, "Solid");
        profile.BorderRadius = request.BorderRadius;
        profile.FooterText = request.FooterText;
        profile.SupportEmail = request.SupportEmail;
        profile.SupportPhone = request.SupportPhone;
        profile.ShowPoweredBy = request.ShowPoweredBy;
        profile.CustomCss = SanitizeCss(request.CustomCss);
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync(userId, "brand.updated", profile.Id, cancellationToken);

        return ToResponse(profile);
    }

    public async Task DeleteAsync(Guid userId, Guid brandId, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);

        var profile = await dbContext.BrandProfiles.SingleOrDefaultAsync(x => x.Id == brandId, cancellationToken)
            ?? throw new InvalidOperationException("Brand profile was not found.");

        profile.DeletedAt = DateTimeOffset.UtcNow;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync(userId, "brand.deleted", profile.Id, cancellationToken);
    }

    public async Task<PublicThemeResponse?> GetPublicThemeAsync(CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
        {
            return null;
        }

        var tenant = await dbContext.Tenants.SingleOrDefaultAsync(x => x.Id == tenantContext.TenantId, cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        var brand = (await dbContext.BrandProfiles.ToListAsync(cancellationToken))
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefault();

        if (brand is null)
        {
            return new PublicThemeResponse(
                tenant.Id,
                tenant.Slug,
                tenant.Name,
                "Platform default",
                "#0f766e",
                "#111827",
                "#c2410c",
                "#ffffff",
                "#111827",
                "Inter",
                "Inter",
                "Solid",
                6,
                null,
                true);
        }

        return new PublicThemeResponse(
            tenant.Id,
            tenant.Slug,
            tenant.Name,
            brand.Name,
            brand.PrimaryColor,
            brand.SecondaryColor,
            brand.AccentColor,
            brand.BackgroundColor,
            brand.TextColor,
            brand.HeadingFont,
            brand.BodyFont,
            brand.ButtonStyle,
            brand.BorderRadius,
            brand.FooterText,
            brand.ShowPoweredBy);
    }

    private async Task EnsureTenantManagerAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
        {
            throw new UnauthorizedAccessException("Tenant context is required.");
        }

        if (await IsPlatformAdminAsync(userId))
        {
            return;
        }

        var allowed = await dbContext.TenantUsers
            .IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == tenantContext.TenantId
                && x.UserId == userId
                && x.Status == TenantUserStatus.Active
                && (x.Role == TenantRole.TenantOwner || x.Role == TenantRole.TenantAdministrator),
                cancellationToken);

        if (!allowed)
        {
            throw new UnauthorizedAccessException("Tenant administrator access is required.");
        }
    }

    private async Task<bool> IsPlatformAdminAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is not null && (await userManager.IsInRoleAsync(user, AppRoles.PlatformOwner) || await userManager.IsInRoleAsync(user, AppRoles.PlatformAdministrator));
    }

    private async Task RecordAuditAsync(Guid userId, string action, Guid entityId, CancellationToken cancellationToken)
    {
        await auditLogger.RecordAsync(new AuditRecord(tenantContext.TenantId, userId, "User", action, "BrandProfile", entityId.ToString(), null, Guid.NewGuid().ToString("N")), cancellationToken);
    }

    private static void Validate(UpsertBrandProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Brand name is required.");
        }

        foreach (var color in new[] { request.PrimaryColor, request.SecondaryColor, request.AccentColor, request.BackgroundColor, request.TextColor })
        {
            if (!HexColorPattern.IsMatch(color))
            {
                throw new InvalidOperationException("Brand colors must be six-digit hex values.");
            }
        }

        if (request.BorderRadius is < 0 or > 32)
        {
            throw new InvalidOperationException("Border radius must be between 0 and 32.");
        }

        _ = SanitizeCss(request.CustomCss);
    }

    private static string? SanitizeCss(string? css)
    {
        if (string.IsNullOrWhiteSpace(css))
        {
            return null;
        }

        if (CssScriptPattern.IsMatch(css))
        {
            throw new InvalidOperationException("Custom CSS cannot contain scripts or executable expressions.");
        }

        return css.Trim();
    }

    private static string DefaultIfBlank(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static BrandProfileResponse ToResponse(BrandProfile profile)
    {
        return new BrandProfileResponse(
            profile.Id,
            profile.TenantId,
            profile.Name,
            profile.PrimaryColor,
            profile.SecondaryColor,
            profile.AccentColor,
            profile.BackgroundColor,
            profile.TextColor,
            profile.HeadingFont,
            profile.BodyFont,
            profile.ButtonStyle,
            profile.BorderRadius,
            profile.FooterText,
            profile.SupportEmail,
            profile.SupportPhone,
            profile.ShowPoweredBy,
            profile.CustomCss,
            profile.CreatedAt,
            profile.UpdatedAt);
    }
}
