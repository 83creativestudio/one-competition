using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Features;

public sealed class TenantFeatureOverride : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid FeatureFlagId { get; set; }
    public bool IsEnabled { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
