using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Competitions;
using OneCompetitions.Contracts.Competitions;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/competitions/{competitionId:guid}/fields")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class CompetitionFieldsController(ICompetitionService competitions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CompetitionFieldResponse>>> Index(Guid competitionId, CancellationToken cancellationToken)
        => Ok(await competitions.ListFieldsAsync(User.GetUserId(), competitionId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<CompetitionFieldResponse>> Create(Guid competitionId, UpsertCompetitionFieldRequest request, CancellationToken cancellationToken)
        => Ok(await competitions.AddFieldAsync(User.GetUserId(), competitionId, request, cancellationToken));

    [HttpPatch("{fieldId:guid}")]
    public async Task<ActionResult<CompetitionFieldResponse>> Update(Guid competitionId, Guid fieldId, UpsertCompetitionFieldRequest request, CancellationToken cancellationToken)
        => Ok(await competitions.UpdateFieldAsync(User.GetUserId(), competitionId, fieldId, request, cancellationToken));

    [HttpDelete("{fieldId:guid}")]
    public async Task<IActionResult> Delete(Guid competitionId, Guid fieldId, CancellationToken cancellationToken)
    {
        await competitions.DeleteFieldAsync(User.GetUserId(), competitionId, fieldId, cancellationToken);
        return NoContent();
    }

    [HttpPost("reorder")]
    public async Task<ActionResult<IReadOnlyList<CompetitionFieldResponse>>> Reorder(Guid competitionId, ReorderCompetitionFieldsRequest request, CancellationToken cancellationToken)
        => Ok(await competitions.ReorderFieldsAsync(User.GetUserId(), competitionId, request, cancellationToken));
}
