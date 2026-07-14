namespace OneCompetitions.Contracts.Campaigns;

public sealed record CreateCampaignSourceRequest(string Name, string SourceType, string Code, string? UtmSource, string? UtmMedium, string? UtmCampaign, string? UtmContent);
