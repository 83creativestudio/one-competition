using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Qr;

public sealed class QrScan : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid QrCodeId { get; set; }
    public Guid CompetitionId { get; set; }
    public Guid CampaignSourceId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? IpHash { get; set; }
    public string? UserAgentHash { get; set; }
    public string? DeviceType { get; set; }
    public string? CountryCode { get; set; }
    public string? Region { get; set; }
    public string? Referrer { get; set; }
    public bool IsUnique { get; set; }
}
