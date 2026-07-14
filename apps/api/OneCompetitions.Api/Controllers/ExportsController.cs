using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Exports;
using OneCompetitions.Contracts.Exports;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/competitions/{competitionId:guid}/exports")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class ExportsController(IExportService exports) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExportJobResponse>>> Index(Guid competitionId, CancellationToken cancellationToken)
        => Ok(await exports.ListAsync(User.GetUserId(), competitionId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ExportJobResponse>> Create(Guid competitionId, CreateExportRequest request, CancellationToken cancellationToken)
        => Ok(await exports.CreateAsync(User.GetUserId(), competitionId, request, cancellationToken));
}
