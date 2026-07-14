using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Competitions;
using OneCompetitions.Contracts.Competitions;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/competitions/{competitionId:guid}")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class CompetitionContentController(ICompetitionService competitions) : ControllerBase
{
    [HttpPost("rules")]
    public async Task<ActionResult<RulesVersionResponse>> CreateRules(Guid competitionId, CreateRulesVersionRequest request, CancellationToken cancellationToken)
        => Ok(await competitions.AddRulesAsync(User.GetUserId(), competitionId, request, cancellationToken));

    [HttpPut("page")]
    public async Task<ActionResult<CompetitionPageResponse>> UpsertPage(Guid competitionId, UpsertCompetitionPageRequest request, CancellationToken cancellationToken)
        => Ok(await competitions.UpsertPageAsync(User.GetUserId(), competitionId, request, cancellationToken));
}
