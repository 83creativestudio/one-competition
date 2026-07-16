namespace OneCompetitions.Contracts.Winners;

public sealed record WinnerActionRequest(string? Notes, string? Channel = null);
