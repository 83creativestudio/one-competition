using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Contracts.Tenants;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/platform/tenants")]
[Authorize(Policy = AppPolicies.PlatformAdmin)]
public sealed class PlatformTenantsController(ITenantAccessService tenants) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TenantSummaryResponse>>> Index(CancellationToken cancellationToken)
    {
        return Ok(await tenants.GetPlatformTenantsAsync(User.GetUserId(), cancellationToken));
    }
}
