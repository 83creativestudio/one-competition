using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Application.Branding;
using OneCompetitions.Application.Storage;
using OneCompetitions.Contracts.Branding;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[AllowAnonymous]
public sealed class PublicBrandsController(IBrandProfileService brands, IAssetService assets) : ControllerBase
{
    [HttpGet("api/public/brands/{brandId:guid}")]
    [HttpGet("c/{tenantSlug}/api/public/brands/{brandId:guid}")]
    public async Task<ActionResult<PublicBrandResponse>> Get(Guid brandId, CancellationToken cancellationToken, int page = 1)
    {
        var result = await brands.GetPublicBrandAsync(brandId, page, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("api/public/media/{assetId:guid}")]
    [HttpGet("c/{tenantSlug}/api/public/media/{assetId:guid}")]
    public async Task<IActionResult> Media(Guid assetId, CancellationToken cancellationToken)
    {
        var result = await assets.OpenPublicMediaAsync(assetId, cancellationToken);
        if (result is null) return NotFound();
        Response.Headers.CacheControl = "public, max-age=60";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(result.Value.Content, result.Value.ContentType, enableRangeProcessing: true);
    }
}
