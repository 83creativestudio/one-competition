using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Tenants;

public sealed class TenantUser : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public TenantRole Role { get; set; }
    public TenantUserStatus Status { get; set; } = TenantUserStatus.Invited;
    public Guid? InvitedByUserId { get; set; }
    public DateTimeOffset? InvitedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public string? InvitationTokenHash { get; set; }
    public DateTimeOffset? InvitationExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Tenant? Tenant { get; set; }
}
