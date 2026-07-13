using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Auditing;

public sealed class AuditEvent : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public required string ActorType { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public string? EntityId { get; set; }
    public Guid? CompetitionId { get; set; }
    public string? IpHash { get; set; }
    public string? UserAgentHash { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public required string CorrelationId { get; set; }
}
