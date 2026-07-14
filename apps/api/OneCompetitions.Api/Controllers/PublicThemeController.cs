using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Application.Branding;
using OneCompetitions.Contracts.Branding;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/public/theme")]
[AllowAnonymous]
public sealed class PublicThemeController(IBrandProfileService brands) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PublicThemeResponse>> Get(CancellationToken cancellationToken)
    {
        var theme = await brands.GetPublicThemeAsync(cancellationToken);
        return theme is null ? NotFound() : Ok(theme);
    }

    [HttpGet("/c/{tenantSlug}/api/public/theme")]
    public async Task<ActionResult<PublicThemeResponse>> GetForPlatformPath(string tenantSlug, CancellationToken cancellationToken)
    {
        var theme = await brands.GetPublicThemeAsync(cancellationToken);
        return theme is null ? NotFound() : Ok(theme);
    }
}
