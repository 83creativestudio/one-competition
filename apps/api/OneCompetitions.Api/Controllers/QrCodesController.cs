using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Campaigns;
using OneCompetitions.Contracts.Qr;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/competitions/{competitionId:guid}/qr-codes")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class QrCodesController(ICampaignService campaigns) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<QrCodeResponse>>> Index(Guid competitionId, CancellationToken cancellationToken)
        => Ok(await campaigns.ListQrCodesAsync(User.GetUserId(), competitionId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<QrCodeResponse>> Create(Guid competitionId, CreateQrCodeRequest request, CancellationToken cancellationToken)
        => Ok(await campaigns.CreateQrCodeAsync(User.GetUserId(), competitionId, request, cancellationToken));
}
