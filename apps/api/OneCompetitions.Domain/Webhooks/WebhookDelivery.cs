namespace OneCompetitions.Domain.Webhooks;

public sealed class WebhookDelivery
{
    public Guid Id { get; set; }
    public Guid WebhookEndpointId { get; set; }
    public Guid WebhookEventId { get; set; }
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public int? LastStatusCode { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
}
