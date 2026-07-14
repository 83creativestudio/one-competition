using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Consents;

public sealed class ConsentDefinition : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid? CompetitionId { get; set; }
    public ConsentType ConsentType { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string Text { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
}
