using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Campaigns;
using OneCompetitions.Contracts.Analytics;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/competitions/{competitionId:guid}/analytics")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class AnalyticsController(ICampaignService campaigns) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<AnalyticsOverviewResponse>> Overview(Guid competitionId, CancellationToken cancellationToken)
        => Ok(await campaigns.OverviewAsync(User.GetUserId(), competitionId, cancellationToken));
}
