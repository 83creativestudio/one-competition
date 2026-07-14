namespace OneCompetitions.Contracts.Competitions;

public sealed record UpsertCompetitionFieldRequest(
    string FieldKey,
    string FieldType,
    string Label,
    string? Placeholder,
    string? HelpText,
    bool IsRequired,
    int DisplayOrder,
    string? ValidationJson,
    string? OptionsJson,
    bool IsSensitive,
    bool IsSearchable,
    bool IsExportable);
