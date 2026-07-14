namespace OneCompetitions.Contracts.Qr;

public sealed record QrCodeResponse(Guid Id, Guid CompetitionId, Guid CampaignSourceId, string ShortCode, string DestinationUrl, string Status, long ScanCount, long UniqueScanCount);
