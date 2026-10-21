using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Contracts.Auth;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.PlatformAdmin)]
[Route("api/platform/users")]
public sealed class PlatformUsersController(IAccountManagementService accounts) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AccountPageResponse>> List([FromQuery] string? search, CancellationToken cancellationToken,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await accounts.ListAsync(User.GetUserId(), search, page, pageSize, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<AccountResponse>> Create(CreateAccountRequest request, CancellationToken cancellationToken) =>
        Ok(await accounts.CreateAsync(User.GetUserId(), request, cancellationToken));

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<AccountResponse>> Update(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken) =>
        Ok(await accounts.UpdateAsync(User.GetUserId(), id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await accounts.DeleteAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }
}
