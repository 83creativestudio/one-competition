using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Application.Competitions;
using OneCompetitions.Application.Entries;
using OneCompetitions.Contracts.Entries;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public")]
public sealed class PublicCompetitionsController(ICompetitionService competitions, IEntryService entries) : ControllerBase
{
    [HttpGet("competitions/{slug}")]
    [HttpGet("/c/{tenantSlug}/api/public/competitions/{slug}")]
    public async Task<ActionResult<PublicCompetitionResponse>> Get(string slug, CancellationToken cancellationToken, string? tenantSlug = null)
    {
        var response = await competitions.GetPublicAsync(slug, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("competitions/{slug}/entries")]
    [HttpPost("/c/{tenantSlug}/api/public/competitions/{slug}/entries")]
    public async Task<ActionResult<EntryResponse>> Submit(string slug, SubmitEntryRequest request, CancellationToken cancellationToken, string? tenantSlug = null)
    {
        var response = await entries.SubmitAsync(slug, request, Request.Headers["X-One-Participant-Session"].ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("entries/{reference}/status")]
    [HttpGet("/c/{tenantSlug}/api/public/entries/{reference}/status")]
    public async Task<ActionResult<EntryResponse>> Status(string reference, CancellationToken cancellationToken, string? tenantSlug = null)
    {
        var response = await entries.GetPublicStatusAsync(reference, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("entries/{reference}/verify-email")]
    [HttpPost("/c/{tenantSlug}/api/public/entries/{reference}/verify-email")]
    public async Task<ActionResult<EntryResponse>> VerifyEmail(string reference, VerifyEntryRequest request, CancellationToken cancellationToken, string? tenantSlug = null) =>
        Ok(await entries.VerifyAsync(reference, "email", request, cancellationToken));

    [HttpPost("entries/{reference}/verify-phone")]
    [HttpPost("/c/{tenantSlug}/api/public/entries/{reference}/verify-phone")]
    public async Task<ActionResult<EntryResponse>> VerifyPhone(string reference, VerifyEntryRequest request, CancellationToken cancellationToken, string? tenantSlug = null) =>
        Ok(await entries.VerifyAsync(reference, "phone", request, cancellationToken));
}
