namespace OneCompetitions.Application.Tenants;

public interface ITenantContextSetter
{
    void Set(Guid tenantId, string tenantSlug, string hostname);
    void Clear(string hostname);
}
