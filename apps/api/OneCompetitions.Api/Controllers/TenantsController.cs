using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Contracts.Tenants;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class TenantsController(ITenantAccessService tenants) : ControllerBase
{
    [HttpGet("current")]
    public async Task<ActionResult<TenantResponse>> Current(CancellationToken cancellationToken)
    {
        var tenant = await tenants.GetCurrentTenantAsync(User.GetUserId(), cancellationToken);
        return tenant is null ? Forbid() : Ok(tenant);
    }

    [HttpGet("current/users")]
    public async Task<ActionResult<IReadOnlyList<TenantUserResponse>>> Users(CancellationToken cancellationToken)
    {
        return Ok(await tenants.GetCurrentTenantUsersAsync(User.GetUserId(), cancellationToken));
    }
}
