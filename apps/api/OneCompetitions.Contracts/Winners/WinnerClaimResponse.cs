namespace OneCompetitions.Contracts.Winners;

public sealed record WinnerClaimResponse(Guid Id, Guid DrawResultId, Guid EntryId, string Status, DateTimeOffset? FirstContactedAt, DateTimeOffset? AcceptedAt, DateTimeOffset? PrizeDeliveredAt, string? DisqualificationReason);
