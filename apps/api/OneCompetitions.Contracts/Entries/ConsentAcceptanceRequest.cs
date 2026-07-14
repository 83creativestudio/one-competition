namespace OneCompetitions.Contracts.Entries;

public sealed record ConsentAcceptanceRequest(Guid ConsentDefinitionId, bool Accepted);
