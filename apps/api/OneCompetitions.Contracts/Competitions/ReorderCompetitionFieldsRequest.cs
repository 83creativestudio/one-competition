namespace OneCompetitions.Contracts.Competitions;

public sealed record ReorderCompetitionFieldsRequest(IReadOnlyList<Guid> FieldIds);
