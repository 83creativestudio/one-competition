namespace OneCompetitions.Contracts.Tenants;

public sealed record TenantResponse(
    Guid Id,
    string Name,
    string LegalName,
    string Slug,
    string Status,
    string DefaultLanguage,
    string TimeZone,
    string CountryCode,
    string Currency);
