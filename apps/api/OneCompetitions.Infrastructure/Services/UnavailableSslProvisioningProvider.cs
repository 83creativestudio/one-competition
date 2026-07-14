using OneCompetitions.Application.Domains;

namespace OneCompetitions.Infrastructure.Services;

public sealed class UnavailableSslProvisioningProvider : ISslProvisioningProvider
{
    public Task<SslProvisioningResult> ProvisionAsync(string hostname, CancellationToken cancellationToken)
    {
        return Task.FromResult(new SslProvisioningResult(false, "SSL provisioning is not implemented in Stage 2.", null));
    }
}
