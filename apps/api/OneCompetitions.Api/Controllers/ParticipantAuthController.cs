using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Application.SocialAuth;
using OneCompetitions.Contracts.SocialAuth;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[AllowAnonymous]
public sealed class ParticipantAuthController(ISocialAuthService socialAuth) : ControllerBase
{
    [HttpGet("api/public/competitions/{slug}/social-auth/providers")]
    [HttpGet("/c/{tenantSlug}/api/public/competitions/{slug}/social-auth/providers")]
    public async Task<ActionResult<IReadOnlyList<SocialAuthProviderResponse>>> Providers(
        string slug, CancellationToken cancellationToken, string? tenantSlug = null) =>
        Ok(await socialAuth.ProvidersAsync(slug, cancellationToken));

    [HttpGet("api/public/competitions/{slug}/social-auth/{provider}/start")]
    [HttpGet("/c/{tenantSlug}/api/public/competitions/{slug}/social-auth/{provider}/start")]
    public async Task<IActionResult> Start(string slug, string provider, [FromQuery] string returnUrl,
        CancellationToken cancellationToken, string? tenantSlug = null)
    {
        var result = await socialAuth.StartAsync(slug, provider, returnUrl, cancellationToken);
        return Redirect(result.AuthorizationUri.ToString());
    }

    [HttpGet("api/participant-auth/{provider}/callback")]
    public async Task<IActionResult> Callback(string provider, [FromQuery] string? code, [FromQuery] string state,
        [FromQuery] string? error, CancellationToken cancellationToken)
    {
        var result = await socialAuth.CompleteProviderCallbackAsync(provider, code, state, error, cancellationToken);
        return Redirect(result.ReturnUri.ToString());
    }

    [HttpPost("api/participant-auth/{provider}/callback")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> FormPostCallback(string provider, [FromForm] string? code, [FromForm] string state,
        [FromForm] string? error, CancellationToken cancellationToken)
    {
        var result = await socialAuth.CompleteProviderCallbackAsync(provider, code, state, error, cancellationToken);
        return Redirect(result.ReturnUri.ToString());
    }

    [HttpPost("api/public/social-auth/complete")]
    [HttpPost("/c/{tenantSlug}/api/public/social-auth/complete")]
    public async Task<ActionResult<SocialAuthSessionResponse>> Complete(CompleteSocialAuthRequest request,
        CancellationToken cancellationToken, string? tenantSlug = null) =>
        Ok(await socialAuth.ExchangeCompletionAsync(request, cancellationToken));

    [HttpPost("api/public/social-actions/{requirementId:guid}/verify")]
    [HttpPost("/c/{tenantSlug}/api/public/social-actions/{requirementId:guid}/verify")]
    public async Task<ActionResult<SocialActionVerificationResponse>> VerifyAction(Guid requirementId,
        CancellationToken cancellationToken, string? tenantSlug = null)
    {
        var token = Request.Headers["X-One-Participant-Session"].ToString();
        return Ok(await socialAuth.VerifyActionAsync(requirementId, token, cancellationToken));
    }
}
