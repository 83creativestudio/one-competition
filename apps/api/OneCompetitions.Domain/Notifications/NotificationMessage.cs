using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Notifications;

public sealed class NotificationMessage : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid? CompetitionId { get; set; }
    public string Channel { get; set; } = "Email";
    public string Recipient { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string BodyText { get; set; } = string.Empty;
    public string? BodyHtml { get; set; }
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTimeOffset AvailableAt { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string? DeduplicationKey { get; set; }
}
