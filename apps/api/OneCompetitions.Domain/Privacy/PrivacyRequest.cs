using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Privacy;

public sealed class PrivacyRequest : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid ParticipantId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string Status { get; set; } = "PendingVerification";
    public string? ResultStorageKey { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
