using OneCompetitions.Application.Auditing;
using OneCompetitions.Domain.Auditing;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class AuditLogger(AppDbContext dbContext) : IAuditLogger
{
    public async Task RecordAsync(AuditRecord record, CancellationToken cancellationToken)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = record.TenantId,
            ActorUserId = record.ActorUserId,
            ActorType = record.ActorType,
            Action = record.Action,
            EntityType = record.EntityType,
            EntityId = record.EntityId,
            MetadataJson = record.MetadataJson,
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = record.CorrelationId
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
