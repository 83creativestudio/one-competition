namespace OneCompetitions.Contracts.Competitions;

public sealed record UpdateCompetitionRequest(
    string Name,
    string? Description,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? EntryLimit,
    int PerParticipantEntryLimit,
    int NumberOfWinners,
    int NumberOfReserveWinners,
    bool RequiresManualApproval);
