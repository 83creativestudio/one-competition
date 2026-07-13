namespace OneCompetitions.Domain.Tenants;

public enum TenantRole
{
    TenantOwner = 0,
    TenantAdministrator = 1,
    CompetitionManager = 2,
    EntryReviewer = 3,
    DrawOperator = 4,
    Auditor = 5,
    Viewer = 6
}
