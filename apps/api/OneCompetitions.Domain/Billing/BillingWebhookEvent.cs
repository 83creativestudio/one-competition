using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Billing;

public sealed class BillingWebhookEvent : TenantScopedEntity
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; }
}
