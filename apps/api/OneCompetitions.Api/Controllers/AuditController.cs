using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Platform;
using OneCompetitions.Contracts.Platform;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class AuditController(IPlatformAdministrationService platform) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditEventResponse>>> List(CancellationToken ct) =>
        Ok(await platform.ListTenantAuditAsync(User.GetUserId(), ct));
}
