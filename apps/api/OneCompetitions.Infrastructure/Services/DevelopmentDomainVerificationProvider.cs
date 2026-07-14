using Microsoft.Extensions.Hosting;
using OneCompetitions.Application.Domains;

namespace OneCompetitions.Infrastructure.Services;

public sealed class DevelopmentDomainVerificationProvider(IHostEnvironment environment) : IDomainVerificationProvider
{
    public Task<DomainVerificationResult> VerifyAsync(string hostname, string token, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            throw new InvalidOperationException("The development domain verification provider cannot run outside development or test environments.");
        }

        var isVerified = hostname.EndsWith(".test", StringComparison.OrdinalIgnoreCase)
            || hostname.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || hostname.Contains("verified", StringComparison.OrdinalIgnoreCase);

        var expectedTarget = $"one-competitions-verify={token}";
        var failure = isVerified ? null : $"Development verification expects a .test/.local hostname or a hostname containing 'verified'. Required TXT: {expectedTarget}";

        return Task.FromResult(new DomainVerificationResult(isVerified, failure, expectedTarget));
    }
}
