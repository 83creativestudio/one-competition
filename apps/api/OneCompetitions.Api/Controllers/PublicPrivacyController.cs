using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Application.Privacy;
using OneCompetitions.Contracts.Privacy;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public/privacy/requests")]
public sealed class PublicPrivacyController(IPrivacyService privacy) : ControllerBase
{
    [HttpPost]
    [HttpPost("/c/{tenantSlug}/api/public/privacy/requests")]
    public async Task<ActionResult<PrivacyRequestAcceptedResponse>> Create(CreatePrivacyRequest request, CancellationToken ct, string? tenantSlug = null)
    {
        await privacy.RequestAsync(request, ct);
        return Accepted(new PrivacyRequestAcceptedResponse("If a matching participant exists, a confirmation message has been queued."));
    }

    [HttpPost("{reference}/complete")]
    [HttpPost("/c/{tenantSlug}/api/public/privacy/requests/{reference}/complete")]
    public async Task<ActionResult<PrivacyRequestResult>> Complete(string reference, CompletePrivacyRequest request, CancellationToken ct, string? tenantSlug = null) =>
        Ok(await privacy.CompleteAsync(reference, request, ct));
}
