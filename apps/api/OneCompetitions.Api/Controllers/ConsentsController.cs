using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Consents;
using OneCompetitions.Contracts.Consents;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/competitions/{competitionId:guid}/consents")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class ConsentsController(IConsentService consents) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConsentDefinitionResponse>>> List(Guid competitionId, CancellationToken ct) => Ok(await consents.ListAsync(User.GetUserId(), competitionId, ct));
    [HttpPost]
    public async Task<ActionResult<ConsentDefinitionResponse>> Create(Guid competitionId, CreateConsentDefinitionRequest request, CancellationToken ct) => Ok(await consents.CreateVersionAsync(User.GetUserId(), competitionId, request, ct));
}
