namespace OneCompetitions.Application.Auditing;

public sealed record AuditRecord(
    Guid TenantId,
    Guid? ActorUserId,
    string ActorType,
    string Action,
    string EntityType,
    string? EntityId,
    string? MetadataJson,
    string CorrelationId);
