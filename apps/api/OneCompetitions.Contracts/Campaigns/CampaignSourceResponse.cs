namespace OneCompetitions.Contracts.Campaigns;

public sealed record CampaignSourceResponse(Guid Id, Guid CompetitionId, string Name, string SourceType, string Code, bool IsActive);
