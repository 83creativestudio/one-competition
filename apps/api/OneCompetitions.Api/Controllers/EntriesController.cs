using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Entries;
using OneCompetitions.Contracts.Entries;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/competitions/{competitionId:guid}/entries")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class EntriesController(IEntryService entries) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EntryResponse>>> Index(Guid competitionId, CancellationToken cancellationToken)
        => Ok(await entries.ListAsync(User.GetUserId(), competitionId, cancellationToken));

    [HttpGet("{entryId:guid}")]
    public async Task<ActionResult<EntryResponse>> Get(Guid competitionId, Guid entryId, CancellationToken cancellationToken)
    {
        var response = await entries.GetAsync(User.GetUserId(), competitionId, entryId, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{entryId:guid}/approve")]
    public async Task<ActionResult<EntryResponse>> Approve(Guid competitionId, Guid entryId, CancellationToken cancellationToken)
        => Ok(await entries.ReviewAsync(User.GetUserId(), competitionId, entryId, new EntryReviewRequest("Approve", null), cancellationToken));

    [HttpPost("{entryId:guid}/reject")]
    public async Task<ActionResult<EntryResponse>> Reject(Guid competitionId, Guid entryId, EntryReviewRequest request, CancellationToken cancellationToken)
        => Ok(await entries.ReviewAsync(User.GetUserId(), competitionId, entryId, request with { Decision = "Reject" }, cancellationToken));

    [HttpPost("{entryId:guid}/mark-duplicate")]
    public async Task<ActionResult<EntryResponse>> Duplicate(Guid competitionId, Guid entryId, EntryReviewRequest request, CancellationToken cancellationToken)
        => Ok(await entries.ReviewAsync(User.GetUserId(), competitionId, entryId, request with { Decision = "MarkDuplicate" }, cancellationToken));

    [HttpPost("{entryId:guid}/disqualify")]
    public async Task<ActionResult<EntryResponse>> Disqualify(Guid competitionId, Guid entryId, EntryReviewRequest request, CancellationToken cancellationToken)
        => Ok(await entries.ReviewAsync(User.GetUserId(), competitionId, entryId, request with { Decision = "Disqualify" }, cancellationToken));
}
