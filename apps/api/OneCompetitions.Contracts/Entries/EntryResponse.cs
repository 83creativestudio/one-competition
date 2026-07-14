namespace OneCompetitions.Contracts.Entries;

public sealed record EntryResponse(
    Guid Id,
    Guid CompetitionId,
    string EntryReference,
    string Status,
    string EligibilityStatus,
    int RiskScore,
    string RiskLevel,
    DateTimeOffset? SubmittedAt,
    string? RejectedReason);
