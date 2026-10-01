using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.SocialAuth;
using OneCompetitions.Contracts.SocialAuth;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/competitions/{competitionId:guid}/social-actions")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class CompetitionSocialActionsController(ISocialAuthService socialAuth) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SocialActionRequirementResponse>>> Index(Guid competitionId, CancellationToken cancellationToken) =>
        Ok(await socialAuth.ListRequirementsAsync(User.GetUserId(), competitionId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<SocialActionRequirementResponse>> Create(Guid competitionId,
        UpsertSocialActionRequirementRequest request, CancellationToken cancellationToken) =>
        Ok(await socialAuth.AddRequirementAsync(User.GetUserId(), competitionId, request, cancellationToken));

    [HttpDelete("{requirementId:guid}")]
    public async Task<IActionResult> Delete(Guid competitionId, Guid requirementId, CancellationToken cancellationToken)
    {
        await socialAuth.DeleteRequirementAsync(User.GetUserId(), competitionId, requirementId, cancellationToken);
        return NoContent();
    }
}
