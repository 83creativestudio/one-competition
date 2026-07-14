using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Webhooks;

public sealed class WebhookEndpoint : TenantScopedEntity
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string SecretHash { get; set; } = string.Empty;
    public string EventTypesJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
