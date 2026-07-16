namespace OneCompetitions.Contracts.Fraud;

public sealed record FraudRuleResponse(Guid Id, string Name, string RuleType, string ConfigurationJson, int ScoreImpact, string Action, bool IsEnabled);
public sealed record UpsertFraudRuleRequest(string Name, string RuleType, string ConfigurationJson, int ScoreImpact, string Action, bool IsEnabled);
public sealed record FraudSummaryResponse(long LowRisk, long MediumRisk, long HighRisk, long Blocked, IReadOnlyList<FraudSignalSummaryResponse> Signals);
public sealed record FraudSignalSummaryResponse(string SignalType, long Count, int TotalScore);
