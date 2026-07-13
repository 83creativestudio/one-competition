using OneCompetitions.Application.Tenants;

namespace OneCompetitions.Infrastructure.Tenancy;

public sealed class TenantContext : ITenantContext, ITenantContextSetter
{
    public Guid TenantId { get; private set; }
    public string TenantSlug { get; private set; } = string.Empty;
    public string Hostname { get; private set; } = string.Empty;
    public bool IsResolved { get; private set; }

    public void Set(Guid tenantId, string tenantSlug, string hostname)
    {
        TenantId = tenantId;
        TenantSlug = tenantSlug;
        Hostname = hostname;
        IsResolved = true;
    }

    public void Clear(string hostname)
    {
        TenantId = Guid.Empty;
        TenantSlug = string.Empty;
        Hostname = hostname;
        IsResolved = false;
    }
}
