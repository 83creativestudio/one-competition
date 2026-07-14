using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Draws;
using OneCompetitions.Contracts.Draws;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/competitions/{competitionId:guid}/draws")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class DrawsController(IDrawService draws) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DrawResponse>>> Index(Guid competitionId, CancellationToken cancellationToken)
        => Ok(await draws.ListAsync(User.GetUserId(), competitionId, cancellationToken));

    [HttpPost("prepare")]
    public async Task<ActionResult<DrawResponse>> Prepare(Guid competitionId, PrepareDrawRequest request, CancellationToken cancellationToken)
        => Ok(await draws.PrepareAsync(User.GetUserId(), competitionId, request, cancellationToken));

    [HttpPost("{drawId:guid}/approve")]
    public async Task<ActionResult<DrawResponse>> Approve(Guid competitionId, Guid drawId, CancellationToken cancellationToken)
        => Ok(await draws.ApproveAsync(User.GetUserId(), competitionId, drawId, cancellationToken));

    [HttpPost("{drawId:guid}/execute")]
    public async Task<ActionResult<DrawResponse>> Execute(Guid competitionId, Guid drawId, CancellationToken cancellationToken)
        => Ok(await draws.ExecuteAsync(User.GetUserId(), competitionId, drawId, cancellationToken));

    [HttpGet("{drawId:guid}/results")]
    public async Task<ActionResult<IReadOnlyList<DrawResultResponse>>> Results(Guid competitionId, Guid drawId, CancellationToken cancellationToken)
        => Ok(await draws.ResultsAsync(User.GetUserId(), competitionId, drawId, cancellationToken));
}
