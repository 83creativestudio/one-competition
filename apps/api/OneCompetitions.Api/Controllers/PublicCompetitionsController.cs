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
    public async Task<ActionResult<PublicCompetitionResponse>> Get(string slug, CancellationToken cancellationToken)
    {
        var response = await competitions.GetPublicAsync(slug, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("competitions/{slug}/entries")]
    public async Task<ActionResult<EntryResponse>> Submit(string slug, SubmitEntryRequest request, CancellationToken cancellationToken)
    {
        var response = await entries.SubmitAsync(slug, request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("entries/{reference}/status")]
    public async Task<ActionResult<EntryResponse>> Status(string reference, CancellationToken cancellationToken)
    {
        var response = await entries.GetPublicStatusAsync(reference, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
