namespace OneCompetitions.Application.Tenants;

/// <summary>
/// Trusted tenant context resolved by server-side middleware.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
    string TenantSlug { get; }
    string Hostname { get; }
    bool IsResolved { get; }
}
