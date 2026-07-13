namespace OneCompetitions.Infrastructure.Identity;

public sealed class UserSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string RefreshTokenHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public ApplicationUser? User { get; set; }
}
