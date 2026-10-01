using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Competitions;

public sealed class CompetitionSocialActionRequirement : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid CompetitionId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string TargetReference { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
