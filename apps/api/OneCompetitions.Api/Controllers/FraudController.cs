using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Fraud;
using OneCompetitions.Contracts.Fraud;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class FraudController(IFraudService fraud) : ControllerBase
{
    [HttpGet("api/fraud-rules")]
    public async Task<ActionResult<IReadOnlyList<FraudRuleResponse>>> Rules(CancellationToken ct) => Ok(await fraud.ListRulesAsync(User.GetUserId(), ct));
    [HttpPost("api/fraud-rules")]
    public async Task<ActionResult<FraudRuleResponse>> Create(UpsertFraudRuleRequest request, CancellationToken ct) => Ok(await fraud.UpsertRuleAsync(User.GetUserId(), null, request, ct));
    [HttpPatch("api/fraud-rules/{ruleId:guid}")]
    public async Task<ActionResult<FraudRuleResponse>> Update(Guid ruleId, UpsertFraudRuleRequest request, CancellationToken ct) => Ok(await fraud.UpsertRuleAsync(User.GetUserId(), ruleId, request, ct));
    [HttpDelete("api/fraud-rules/{ruleId:guid}")]
    public async Task<IActionResult> Delete(Guid ruleId, CancellationToken ct) { await fraud.DeleteRuleAsync(User.GetUserId(), ruleId, ct); return NoContent(); }
    [HttpGet("api/competitions/{competitionId:guid}/fraud-summary")]
    public async Task<ActionResult<FraudSummaryResponse>> Summary(Guid competitionId, CancellationToken ct) => Ok(await fraud.SummaryAsync(User.GetUserId(), competitionId, ct));
}
