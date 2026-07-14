using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Branding;
using OneCompetitions.Contracts.Branding;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/brands")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class BrandsController(IBrandProfileService brands) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BrandProfileResponse>>> Index(CancellationToken cancellationToken)
    {
        return Ok(await brands.ListAsync(User.GetUserId(), cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<BrandProfileResponse>> Create(UpsertBrandProfileRequest request, CancellationToken cancellationToken)
    {
        var brand = await brands.CreateAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = brand.Id }, brand);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BrandProfileResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var brand = await brands.GetAsync(User.GetUserId(), id, cancellationToken);
        return brand is null ? NotFound() : Ok(brand);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<BrandProfileResponse>> Update(Guid id, UpsertBrandProfileRequest request, CancellationToken cancellationToken)
    {
        return Ok(await brands.UpdateAsync(User.GetUserId(), id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await brands.DeleteAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }
}
