using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Campaigns;
using OneCompetitions.Contracts.Campaigns;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/competitions/{competitionId:guid}/sources")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class CampaignsController(ICampaignService campaigns) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CampaignSourceResponse>>> Index(Guid competitionId, CancellationToken cancellationToken)
        => Ok(await campaigns.ListSourcesAsync(User.GetUserId(), competitionId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<CampaignSourceResponse>> Create(Guid competitionId, CreateCampaignSourceRequest request, CancellationToken cancellationToken)
        => Ok(await campaigns.CreateSourceAsync(User.GetUserId(), competitionId, request, cancellationToken));
}
