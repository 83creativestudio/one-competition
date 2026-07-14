using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Privacy;

public sealed class DataRetentionPolicy : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid? CompetitionId { get; set; }
    public int ParticipantRetentionDays { get; set; } = 365;
    public int UploadRetentionDays { get; set; } = 365;
    public int AuditRetentionDays { get; set; } = 2555;
    public int ExportRetentionDays { get; set; } = 14;
    public int AnonymiseAfterDays { get; set; } = 365;
    public int DeleteUploadsAfterDays { get; set; } = 365;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
