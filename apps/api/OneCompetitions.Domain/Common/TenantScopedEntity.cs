namespace OneCompetitions.Domain.Common;

public abstract class TenantScopedEntity
{
    public Guid TenantId { get; set; }
}
