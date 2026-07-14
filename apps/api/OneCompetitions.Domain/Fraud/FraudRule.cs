using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Fraud;

public sealed class FraudRule : TenantScopedEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public string ConfigurationJson { get; set; } = "{}";
    public int ScoreImpact { get; set; }
    public string Action { get; set; } = "Review";
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
