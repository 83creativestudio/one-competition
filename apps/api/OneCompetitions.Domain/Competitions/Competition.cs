using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Competitions;

public sealed class Competition : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid? BrandProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? InternalReference { get; set; }
    public string? Description { get; set; }
    public CompetitionStatus Status { get; set; } = CompetitionStatus.Draft;
    public CompetitionType CompetitionType { get; set; } = CompetitionType.StandardDraw;
    public string DefaultLanguage { get; set; } = "en";
    public string TimeZone { get; set; } = "UTC";
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public DateTimeOffset? DrawScheduledAt { get; set; }
    public int? EntryLimit { get; set; }
    public int PerParticipantEntryLimit { get; set; } = 1;
    public int NumberOfWinners { get; set; } = 1;
    public int NumberOfReserveWinners { get; set; }
    public string EligibilityMode { get; set; } = "Standard";
    public int? MinimumAge { get; set; }
    public string AllowedCountryCodesJson { get; set; } = "[]";
    public bool RequiresEmailVerification { get; set; }
    public bool RequiresPhoneVerification { get; set; }
    public bool RequiresManualApproval { get; set; } = true;
    public bool AllowGuestEntry { get; set; } = true;
    public bool EnableReferrals { get; set; }
    public bool EnablePublicResults { get; set; }
    public bool EnableLiveCounter { get; set; }
    public bool RequireDrawApproval { get; set; } = true;
    public bool RequireSeparateDrawOperator { get; set; } = true;
    public bool RequireAuditorPresence { get; set; }
    public Guid? RulesVersionId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? PublishedByUserId { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
