namespace OneCompetitions.Application.Auditing;

/// <summary>
/// Writes append-only audit records for security-relevant actions.
/// </summary>
public interface IAuditLogger
{
    Task RecordAsync(AuditRecord record, CancellationToken cancellationToken);
}
