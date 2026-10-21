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
    [HttpGet("page")]
    public async Task<ActionResult<CompetitionPageResponse>> GetPage(Guid competitionId, CancellationToken cancellationToken, string language = "en")
    {
        var page = await competitions.GetPageAsync(User.GetUserId(), competitionId, language, cancellationToken);
        return page is null ? NotFound() : Ok(page);
    }

    [HttpPut("brand")]
    public async Task<ActionResult<CompetitionResponse>> SetBrand(Guid competitionId, SetCompetitionBrandRequest request, CancellationToken cancellationToken)
        => Ok(await competitions.SetBrandAsync(User.GetUserId(), competitionId, request.BrandProfileId, cancellationToken));
    [HttpPost("rules")]
    public async Task<ActionResult<RulesVersionResponse>> CreateRules(Guid competitionId, CreateRulesVersionRequest request, CancellationToken cancellationToken)
        => Ok(await competitions.AddRulesAsync(User.GetUserId(), competitionId, request, cancellationToken));

    [HttpPut("page")]
    public async Task<ActionResult<CompetitionPageResponse>> UpsertPage(Guid competitionId, UpsertCompetitionPageRequest request, CancellationToken cancellationToken)
        => Ok(await competitions.UpsertPageAsync(User.GetUserId(), competitionId, request, cancellationToken));
}
