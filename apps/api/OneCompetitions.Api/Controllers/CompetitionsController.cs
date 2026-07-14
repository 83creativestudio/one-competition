using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Competitions;
using OneCompetitions.Contracts.Competitions;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/competitions")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class CompetitionsController(ICompetitionService competitions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CompetitionResponse>>> Index(CancellationToken cancellationToken)
        => Ok(await competitions.ListAsync(User.GetUserId(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<CompetitionResponse>> Create(CreateCompetitionRequest request, CancellationToken cancellationToken)
    {
        var response = await competitions.CreateAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompetitionResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var response = await competitions.GetAsync(User.GetUserId(), id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<CompetitionResponse>> Update(Guid id, UpdateCompetitionRequest request, CancellationToken cancellationToken)
        => Ok(await competitions.UpdateAsync(User.GetUserId(), id, request, cancellationToken));

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<CompetitionResponse>> Publish(Guid id, CancellationToken cancellationToken)
        => Ok(await competitions.PublishAsync(User.GetUserId(), id, cancellationToken));

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<CompetitionResponse>> Close(Guid id, [FromBody] string? reason, CancellationToken cancellationToken)
        => Ok(await competitions.CloseAsync(User.GetUserId(), id, reason, cancellationToken));

    [HttpGet("{id:guid}/versions")]
    public async Task<ActionResult<IReadOnlyList<CompetitionVersionResponse>>> Versions(Guid id, CancellationToken cancellationToken)
        => Ok(await competitions.VersionsAsync(User.GetUserId(), id, cancellationToken));
}
