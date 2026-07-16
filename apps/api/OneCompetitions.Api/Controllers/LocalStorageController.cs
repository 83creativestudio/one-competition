using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Storage;
using OneCompetitions.Application.Tenants;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/local-storage")]
[Authorize(Policy = AppPolicies.TenantMember)]
public sealed class LocalStorageController(
    IObjectStorage storage,
    ITenantContext tenantContext,
    IConfiguration configuration,
    IHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Download([FromQuery] string key, CancellationToken cancellationToken)
    {
        var local = string.Equals(configuration["OBJECT_STORAGE_PROVIDER"], "local", StringComparison.OrdinalIgnoreCase)
            || (string.IsNullOrWhiteSpace(configuration["OBJECT_STORAGE_PROVIDER"]) && (environment.IsDevelopment() || environment.IsEnvironment("Testing")));
        if (!local) return NotFound();
        var prefix = $"tenant/{tenantContext.TenantId:N}/";
        if (!key.StartsWith(prefix, StringComparison.Ordinal) || key.Contains("..", StringComparison.Ordinal)) return Forbid();
        if (!await storage.ExistsAsync(key, cancellationToken)) return NotFound();
        return File(await storage.OpenReadAsync(key, cancellationToken), "application/octet-stream", Path.GetFileName(key));
    }
}
