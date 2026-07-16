using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Billing;

public sealed class TenantSubscription : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public string Status { get; set; } = "Trial";
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? TrialEndsAt { get; set; }
    public DateTimeOffset CurrentPeriodStartsAt { get; set; }
    public DateTimeOffset CurrentPeriodEndsAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? ExternalProvider { get; set; }
    public string? ExternalSubscriptionId { get; set; }
    public string? ExternalCustomerId { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
