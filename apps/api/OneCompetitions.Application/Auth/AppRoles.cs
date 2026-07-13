namespace OneCompetitions.Application.Auth;

public static class AppRoles
{
    public const string PlatformOwner = "PlatformOwner";
    public const string PlatformAdministrator = "PlatformAdministrator";
    public const string PlatformSupport = "PlatformSupport";
    public const string ResellerAdministrator = "ResellerAdministrator";
    public const string TenantOwner = "TenantOwner";
    public const string TenantAdministrator = "TenantAdministrator";
    public const string CompetitionManager = "CompetitionManager";
    public const string EntryReviewer = "EntryReviewer";
    public const string DrawOperator = "DrawOperator";
    public const string Auditor = "Auditor";
    public const string Viewer = "Viewer";

    public static readonly string[] All =
    [
        PlatformOwner,
        PlatformAdministrator,
        PlatformSupport,
        ResellerAdministrator,
        TenantOwner,
        TenantAdministrator,
        CompetitionManager,
        EntryReviewer,
        DrawOperator,
        Auditor,
        Viewer
    ];

    public static readonly string[] PlatformRoles =
    [
        PlatformOwner,
        PlatformAdministrator,
        PlatformSupport,
        ResellerAdministrator
    ];
}
