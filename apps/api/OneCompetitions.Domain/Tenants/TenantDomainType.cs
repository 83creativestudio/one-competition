namespace OneCompetitions.Domain.Tenants;

public enum TenantDomainType
{
    PlatformPath = 0,
    PlatformSubdomain = 1,
    CustomSubdomain = 2,
    CustomRootDomain = 3,
    CustomWwwDomain = 4
}
