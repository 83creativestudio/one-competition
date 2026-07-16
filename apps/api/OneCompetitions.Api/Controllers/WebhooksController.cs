using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Webhooks;
using OneCompetitions.Contracts.Webhooks;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class WebhooksController(IWebhookService webhooks) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WebhookEndpointResponse>>> Index(CancellationToken cancellationToken) =>
        Ok(await webhooks.ListAsync(User.GetUserId(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<WebhookEndpointCreatedResponse>> Create(CreateWebhookEndpointRequest request, CancellationToken cancellationToken) =>
        Ok(await webhooks.CreateAsync(User.GetUserId(), request, cancellationToken));

    [HttpDelete("{endpointId:guid}")]
    public async Task<IActionResult> Delete(Guid endpointId, CancellationToken cancellationToken)
    {
        await webhooks.DeleteAsync(User.GetUserId(), endpointId, cancellationToken);
        return NoContent();
    }
}
