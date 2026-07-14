using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Domains;
using OneCompetitions.Contracts.Domains;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/domains")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class DomainsController(ITenantDomainService domains) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TenantDomainResponse>>> Index(CancellationToken cancellationToken)
    {
        return Ok(await domains.ListAsync(User.GetUserId(), cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<TenantDomainResponse>> Create(CreateTenantDomainRequest request, CancellationToken cancellationToken)
    {
        var domain = await domains.CreateAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = domain.Id }, domain);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantDomainResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var domain = (await domains.ListAsync(User.GetUserId(), cancellationToken)).SingleOrDefault(x => x.Id == id);
        return domain is null ? NotFound() : Ok(domain);
    }

    [HttpPost("{id:guid}/verify")]
    public async Task<ActionResult<DomainVerificationResponse>> Verify(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await domains.VerifyAsync(User.GetUserId(), id, cancellationToken));
    }

    [HttpPost("{id:guid}/set-primary")]
    public async Task<ActionResult<TenantDomainResponse>> SetPrimary(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await domains.SetPrimaryAsync(User.GetUserId(), id, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await domains.DeleteAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }
}
