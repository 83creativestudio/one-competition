namespace OneCompetitions.Contracts.Exports;

public sealed record ExportJobResponse(Guid Id, Guid? CompetitionId, string ExportType, string Format, string Status, string? StorageKey, DateTimeOffset ExpiresAt, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);
