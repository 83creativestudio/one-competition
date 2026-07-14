using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Winners;
using OneCompetitions.Contracts.Winners;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/competitions/{competitionId:guid}/winners")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class WinnersController(IWinnerService winners) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WinnerClaimResponse>>> Index(Guid competitionId, CancellationToken cancellationToken)
        => Ok(await winners.ListAsync(User.GetUserId(), competitionId, cancellationToken));

    [HttpPost("{winnerId:guid}/contact")]
    public async Task<ActionResult<WinnerClaimResponse>> Contact(Guid competitionId, Guid winnerId, WinnerActionRequest request, CancellationToken cancellationToken)
        => Ok(await winners.ContactAsync(User.GetUserId(), competitionId, winnerId, request, cancellationToken));

    [HttpPost("{winnerId:guid}/accept")]
    public async Task<ActionResult<WinnerClaimResponse>> Accept(Guid competitionId, Guid winnerId, CancellationToken cancellationToken)
        => Ok(await winners.AcceptAsync(User.GetUserId(), competitionId, winnerId, cancellationToken));

    [HttpPost("{winnerId:guid}/disqualify")]
    public async Task<ActionResult<WinnerClaimResponse>> Disqualify(Guid competitionId, Guid winnerId, WinnerActionRequest request, CancellationToken cancellationToken)
        => Ok(await winners.DisqualifyAsync(User.GetUserId(), competitionId, winnerId, request, cancellationToken));

    [HttpPost("{winnerId:guid}/deliver-prize")]
    public async Task<ActionResult<WinnerClaimResponse>> Deliver(Guid competitionId, Guid winnerId, CancellationToken cancellationToken)
        => Ok(await winners.DeliverPrizeAsync(User.GetUserId(), competitionId, winnerId, cancellationToken));
}
