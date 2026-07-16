using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Entries;

public sealed class EntryVerification : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid EntryId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
