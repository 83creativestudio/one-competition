using OneCompetitions.Contracts.Fraud;

namespace OneCompetitions.Application.Fraud;

public sealed record FraudEvaluationRequest(
    Guid CompetitionId,
    string? Email,
    string? Phone,
    string? IpHash,
    string? DeviceFingerprintHash,
    DateTimeOffset? FormStartedAt,
    DateTimeOffset SubmittedAt);

public sealed record FraudSignalResult(Guid? RuleId, string SignalType, int ScoreImpact, string Description, string EvidenceJson, string Action);
public sealed record FraudEvaluationResult(int Score, bool ShouldBlock, IReadOnlyList<FraudSignalResult> Signals);

public interface IFraudService
{
    Task<FraudEvaluationResult> EvaluateAsync(FraudEvaluationRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<FraudRuleResponse>> ListRulesAsync(Guid userId, CancellationToken cancellationToken);
    Task<FraudRuleResponse> UpsertRuleAsync(Guid userId, Guid? ruleId, UpsertFraudRuleRequest request, CancellationToken cancellationToken);
    Task DeleteRuleAsync(Guid userId, Guid ruleId, CancellationToken cancellationToken);
    Task<FraudSummaryResponse> SummaryAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken);
}
