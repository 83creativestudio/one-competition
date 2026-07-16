using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Billing;
using OneCompetitions.Contracts.Billing;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class BillingController(IBillingService billing) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<BillingOverviewResponse>> Overview(CancellationToken cancellationToken) =>
        Ok(await billing.GetOverviewAsync(User.GetUserId(), cancellationToken));

    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutSessionResponse>> Checkout(CreateCheckoutSessionRequest request, CancellationToken cancellationToken) =>
        Ok(await billing.CreateCheckoutAsync(User.GetUserId(), request, cancellationToken));

    [HttpPost("stripe/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        await billing.HandleWebhookAsync(payload, Request.Headers["Stripe-Signature"].ToString(), cancellationToken);
        return Ok();
    }
}
