namespace OneCompetitions.Domain.Tenants;

public enum TenantDomainStatus
{
    Pending = 0,
    AwaitingDns = 1,
    Verifying = 2,
    Verified = 3,
    ProvisioningSsl = 4,
    Active = 5,
    DnsError = 6,
    SslError = 7,
    Suspended = 8,
    Disconnected = 9
}
