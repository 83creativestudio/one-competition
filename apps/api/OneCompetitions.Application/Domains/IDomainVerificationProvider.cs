namespace OneCompetitions.Application.Domains;

/// <summary>
/// Verifies customer domain ownership against provider-specific DNS records.
/// </summary>
public interface IDomainVerificationProvider
{
    Task<DomainVerificationResult> VerifyAsync(string hostname, string token, CancellationToken cancellationToken);
}
