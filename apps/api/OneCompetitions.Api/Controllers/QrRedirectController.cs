using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Application.Campaigns;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("q")]
public sealed class QrRedirectController(ICampaignService campaigns) : ControllerBase
{
    [HttpGet("{shortCode}")]
    public async Task<IActionResult> RedirectQr(string shortCode, CancellationToken cancellationToken)
    {
        var destination = await campaigns.RecordQrScanAndGetRedirectAsync(shortCode, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), Request.Headers.Referer.ToString(), cancellationToken);
        return Redirect(destination);
    }
}
