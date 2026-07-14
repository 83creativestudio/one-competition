namespace OneCompetitions.Contracts.Draws;

public sealed record DrawResultResponse(Guid Id, Guid DrawId, Guid EntryId, string ResultType, int Position, string EntryReference, DateTimeOffset SelectedAt);
