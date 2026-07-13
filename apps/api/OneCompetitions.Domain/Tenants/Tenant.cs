namespace OneCompetitions.Domain.Tenants;

public sealed class Tenant
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string LegalName { get; set; }
    public required string Slug { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Trial;
    public string DefaultLanguage { get; set; } = "en";
    public string TimeZone { get; set; } = "UTC";
    public string CountryCode { get; set; } = "CY";
    public string Currency { get; set; } = "EUR";
    public Guid? PlanId { get; set; }
    public Guid? ResellerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? SuspendedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public List<TenantUser> Users { get; set; } = [];
    public List<TenantDomain> Domains { get; set; } = [];
}
