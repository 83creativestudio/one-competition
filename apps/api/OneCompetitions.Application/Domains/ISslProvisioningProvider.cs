namespace OneCompetitions.Application.Domains;

/// <summary>
/// Provisions SSL certificates for verified custom domains.
/// </summary>
public interface ISslProvisioningProvider
{
    Task<SslProvisioningResult> ProvisionAsync(string hostname, CancellationToken cancellationToken);
}
