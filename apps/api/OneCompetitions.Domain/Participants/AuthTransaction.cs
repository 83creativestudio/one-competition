namespace OneCompetitions.Domain.Participants;

public sealed class AuthTransaction
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompetitionId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string StateHash { get; set; } = string.Empty;
    public string PkceChallenge { get; set; } = string.Empty;
    public string PkceVerifierCiphertext { get; set; } = string.Empty;
    public string NonceHash { get; set; } = string.Empty;
    public string NonceCiphertext { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
