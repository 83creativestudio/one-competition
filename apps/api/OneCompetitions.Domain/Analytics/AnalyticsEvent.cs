using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Analytics;

public sealed class AnalyticsEvent : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid CompetitionId { get; set; }
    public string? SessionIdHash { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid? CampaignSourceId { get; set; }
    public string PropertiesJson { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
}
