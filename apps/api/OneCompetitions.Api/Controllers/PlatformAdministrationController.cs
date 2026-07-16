using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Platform;
using OneCompetitions.Contracts.Billing;
using OneCompetitions.Contracts.Platform;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/platform")]
[Authorize(Policy = AppPolicies.PlatformAdmin)]
public sealed class PlatformAdministrationController(IPlatformAdministrationService platform) : ControllerBase
{
    [HttpGet("tenants/{tenantId:guid}")]
    public async Task<ActionResult<PlatformTenantDetailResponse>> Tenant(Guid tenantId, CancellationToken ct)
    {
        var result = await platform.GetTenantAsync(tenantId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("tenants/{tenantId:guid}/status")]
    public async Task<IActionResult> TenantStatus(Guid tenantId, UpdateTenantStatusRequest request, CancellationToken ct)
    {
        await platform.UpdateTenantStatusAsync(User.GetUserId(), tenantId, request, ct); return NoContent();
    }

    [HttpGet("domains")]
    public async Task<ActionResult<IReadOnlyList<PlatformDomainResponse>>> Domains(CancellationToken ct) => Ok(await platform.ListDomainsAsync(ct));

    [HttpGet("features")]
    public async Task<ActionResult<IReadOnlyList<FeatureFlagResponse>>> Features(CancellationToken ct) => Ok(await platform.ListFeaturesAsync(ct));
    [HttpPost("features")]
    public async Task<ActionResult<FeatureFlagResponse>> CreateFeature(UpsertFeatureFlagRequest request, CancellationToken ct) => Ok(await platform.UpsertFeatureAsync(null, request, ct));
    [HttpPatch("features/{id:guid}")]
    public async Task<ActionResult<FeatureFlagResponse>> UpdateFeature(Guid id, UpsertFeatureFlagRequest request, CancellationToken ct) => Ok(await platform.UpsertFeatureAsync(id, request, ct));

    [HttpGet("plans")]
    public async Task<ActionResult<IReadOnlyList<PlanResponse>>> Plans(CancellationToken ct) => Ok(await platform.ListPlansAsync(ct));
    [HttpPost("plans")]
    public async Task<ActionResult<PlanResponse>> CreatePlan(UpsertPlanRequest request, CancellationToken ct) => Ok(await platform.UpsertPlanAsync(null, request, ct));
    [HttpPatch("plans/{id:guid}")]
    public async Task<ActionResult<PlanResponse>> UpdatePlan(Guid id, UpsertPlanRequest request, CancellationToken ct) => Ok(await platform.UpsertPlanAsync(id, request, ct));

    [HttpGet("resellers")]
    public async Task<ActionResult<IReadOnlyList<ResellerResponse>>> Resellers(CancellationToken ct) => Ok(await platform.ListResellersAsync(ct));
    [HttpPost("resellers")]
    public async Task<ActionResult<ResellerResponse>> CreateReseller(UpsertResellerRequest request, CancellationToken ct) => Ok(await platform.UpsertResellerAsync(null, request, ct));
    [HttpPatch("resellers/{id:guid}")]
    public async Task<ActionResult<ResellerResponse>> UpdateReseller(Guid id, UpsertResellerRequest request, CancellationToken ct) => Ok(await platform.UpsertResellerAsync(id, request, ct));

    [HttpGet("audit")]
    public async Task<ActionResult<IReadOnlyList<AuditEventResponse>>> Audit(CancellationToken ct) => Ok(await platform.ListPlatformAuditAsync(ct));
}
