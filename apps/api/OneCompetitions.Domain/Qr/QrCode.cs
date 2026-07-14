using OneCompetitions.Domain.Common;

namespace OneCompetitions.Domain.Qr;

public sealed class QrCode : TenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid CompetitionId { get; set; }
    public Guid CampaignSourceId { get; set; }
    public string ShortCode { get; set; } = string.Empty;
    public string DestinationUrl { get; set; } = string.Empty;
    public string StyleJson { get; set; } = "{}";
    public QrCodeStatus Status { get; set; } = QrCodeStatus.Active;
    public long ScanCount { get; set; }
    public long UniqueScanCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}
