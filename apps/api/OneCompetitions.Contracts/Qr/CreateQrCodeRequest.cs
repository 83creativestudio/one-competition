namespace OneCompetitions.Contracts.Qr;

public sealed record CreateQrCodeRequest(Guid CampaignSourceId, string DestinationUrl, string? StyleJson, DateTimeOffset? ExpiresAt);
