using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Application.Draws;
using OneCompetitions.Contracts.Draws;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/public/draws")]
[AllowAnonymous]
public sealed class PublicDrawVerificationController(IDrawCertificateService certificates) : ControllerBase
{
    [HttpGet("{drawReference}/verification")]
    public async Task<ActionResult<DrawVerificationResponse>> Verify(string drawReference, CancellationToken cancellationToken)
    {
        var result = await certificates.GetPublicVerificationAsync(drawReference, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
