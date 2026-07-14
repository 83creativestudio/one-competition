using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Webhooks;

public sealed class WebhookEvent : TenantScopedEntity
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
}
