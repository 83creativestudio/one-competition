using OneCompetitions.Domain.Tenants;

namespace OneCompetitions.Domain.Tests;

public sealed class TenantDomainTests
{
    [Fact]
    public void Active_platform_subdomain_belongs_to_a_tenant()
    {
        var tenantId = Guid.NewGuid();
        var domain = new TenantDomain
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Hostname = "one-digital.competitions.local",
            DomainType = TenantDomainType.PlatformSubdomain,
            Status = TenantDomainStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        Assert.Equal(tenantId, domain.TenantId);
        Assert.Equal(TenantDomainType.PlatformSubdomain, domain.DomainType);
        Assert.Equal(TenantDomainStatus.Active, domain.Status);
    }
}
