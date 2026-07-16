namespace OneCompetitions.Contracts.Tenants;

public sealed record UpdateTenantRequest(
    string Name,
    string LegalName,
    string DefaultLanguage,
    string TimeZone,
    string CountryCode,
    string Currency);

public sealed record InviteTenantUserRequest(string Email, string Role);
public sealed record UpdateTenantUserRequest(string Role, string Status);
