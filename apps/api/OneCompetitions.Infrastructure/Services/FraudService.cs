using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneCompetitions.Application.Auditing;
using OneCompetitions.Application.Fraud;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Contracts.Fraud;
using OneCompetitions.Domain.Entries;
using OneCompetitions.Domain.Fraud;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class FraudService(AppDbContext dbContext, ITenantContext tenantContext, UserManager<ApplicationUser> userManager, IAuditLogger auditLogger)
    : TenantScopedServiceBase(dbContext, tenantContext, userManager), IFraudService
{
    private sealed record RuleDefinition(Guid? Id, string Type, int Score, string Action, JsonElement Configuration);

    public async Task<FraudEvaluationResult> EvaluateAsync(FraudEvaluationRequest request, CancellationToken cancellationToken)
    {
        var configured = await DbContext.FraudRules.Where(x => x.IsEnabled).ToListAsync(cancellationToken);
        var rules = configured.Select(ToDefinition).ToDictionary(x => x.Type, StringComparer.OrdinalIgnoreCase);
        var signals = new List<FraudSignalResult>();
        var sinceHour = request.SubmittedAt.AddHours(-1);
        var sinceMinute = request.SubmittedAt.AddMinutes(-1);

        async Task AddWhenAsync(string type, bool condition, string description, object evidence, int defaultScore, string defaultAction = "Review")
        {
            if (!condition) return;
            rules.TryGetValue(type, out var rule);
            if (configured.Count > 0 && rule is null) return;
            signals.Add(new FraudSignalResult(rule?.Id, type, rule?.Score ?? defaultScore, description, JsonSerializer.Serialize(evidence), rule?.Action ?? defaultAction));
            await Task.CompletedTask;
        }

        var duplicateEmail = !string.IsNullOrWhiteSpace(request.Email) && await DbContext.CompetitionEntries.AnyAsync(x => x.CompetitionId == request.CompetitionId
            && DbContext.Participants.Any(p => p.Id == x.ParticipantId && p.PrimaryEmail == request.Email), cancellationToken);
        var duplicatePhone = !string.IsNullOrWhiteSpace(request.Phone) && await DbContext.CompetitionEntries.AnyAsync(x => x.CompetitionId == request.CompetitionId
            && DbContext.Participants.Any(p => p.Id == x.ParticipantId && p.PrimaryPhone == request.Phone), cancellationToken);
        await AddWhenAsync("DuplicateEmail", duplicateEmail, "Email has already entered this competition.", new { duplicate = true }, 40);
        await AddWhenAsync("DuplicatePhone", duplicatePhone, "Phone has already entered this competition.", new { duplicate = true }, 40);

        int ipHour;
        int ipMinute;
        int deviceHour;
        if (DbContext.Database.IsSqlite())
        {
            var recent = await DbContext.CompetitionEntries.Where(x => x.CompetitionId == request.CompetitionId).ToListAsync(cancellationToken);
            ipHour = string.IsNullOrWhiteSpace(request.IpHash) ? 0 : recent.Count(x => x.IpHash == request.IpHash && x.CreatedAt >= sinceHour);
            ipMinute = string.IsNullOrWhiteSpace(request.IpHash) ? 0 : recent.Count(x => x.IpHash == request.IpHash && x.CreatedAt >= sinceMinute);
            deviceHour = string.IsNullOrWhiteSpace(request.DeviceFingerprintHash) ? 0 : recent.Count(x => x.DeviceFingerprintHash == request.DeviceFingerprintHash && x.CreatedAt >= sinceHour);
        }
        else
        {
            ipHour = string.IsNullOrWhiteSpace(request.IpHash) ? 0 : await DbContext.CompetitionEntries.CountAsync(x => x.CompetitionId == request.CompetitionId && x.IpHash == request.IpHash && x.CreatedAt >= sinceHour, cancellationToken);
            ipMinute = string.IsNullOrWhiteSpace(request.IpHash) ? 0 : await DbContext.CompetitionEntries.CountAsync(x => x.CompetitionId == request.CompetitionId && x.IpHash == request.IpHash && x.CreatedAt >= sinceMinute, cancellationToken);
            deviceHour = string.IsNullOrWhiteSpace(request.DeviceFingerprintHash) ? 0 : await DbContext.CompetitionEntries.CountAsync(x => x.CompetitionId == request.CompetitionId && x.DeviceFingerprintHash == request.DeviceFingerprintHash && x.CreatedAt >= sinceHour, cancellationToken);
        }
        await AddWhenAsync("ExcessiveIp", ipHour >= Threshold(rules, "ExcessiveIp", "threshold", 20), "High entry volume from one network.", new { count = ipHour, windowMinutes = 60 }, 25);
        await AddWhenAsync("SuspiciousVelocity", ipMinute >= Threshold(rules, "SuspiciousVelocity", "threshold", 8), "Suspicious submission velocity detected.", new { count = ipMinute, windowMinutes = 1 }, 35);
        await AddWhenAsync("ExcessiveDevice", deviceHour >= Threshold(rules, "ExcessiveDevice", "threshold", 5), "High entry volume from one device.", new { count = deviceHour, windowMinutes = 60 }, 35);

        var domain = request.Email?.Split('@').LastOrDefault()?.ToLowerInvariant();
        var disposable = Values(rules, "DisposableEmail", "domains", ["mailinator.com", "10minutemail.com", "guerrillamail.com"]);
        await AddWhenAsync("DisposableEmail", domain is not null && disposable.Contains(domain, StringComparer.OrdinalIgnoreCase), "Disposable email provider detected.", new { domain }, 30);
        var minSeconds = Threshold(rules, "TooFast", "minimumSeconds", 3);
        await AddWhenAsync("TooFast", request.FormStartedAt is not null && request.SubmittedAt - request.FormStartedAt < TimeSpan.FromSeconds(minSeconds), "Form was completed unusually quickly.", new { minimumSeconds = minSeconds }, 20);
        await AddWhenAsync("InvalidPhone", !string.IsNullOrWhiteSpace(request.Phone) && !Regex.IsMatch(request.Phone, @"^\+?[0-9][0-9\s()-]{6,24}$", RegexOptions.CultureInvariant), "Phone number format is invalid.", new { invalid = true }, 25);

        var score = signals.Sum(x => x.ScoreImpact);
        return new FraudEvaluationResult(score, signals.Any(x => x.Action.Equals("Block", StringComparison.OrdinalIgnoreCase)), signals);
    }

    public async Task<IReadOnlyList<FraudRuleResponse>> ListRulesAsync(Guid userId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        return await DbContext.FraudRules.OrderBy(x => x.Name).Select(x => ToResponse(x)).ToListAsync(cancellationToken);
    }

    public async Task<FraudRuleResponse> UpsertRuleAsync(Guid userId, Guid? ruleId, UpsertFraudRuleRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        _ = JsonDocument.Parse(request.ConfigurationJson);
        if (request.ScoreImpact is < 0 or > 100) throw new InvalidOperationException("Score impact must be between 0 and 100.");
        if (request.Action is not ("Review" or "Block" or "Flag")) throw new InvalidOperationException("Fraud action must be Review, Flag, or Block.");
        var rule = ruleId is null ? null : await DbContext.FraudRules.SingleOrDefaultAsync(x => x.Id == ruleId, cancellationToken);
        rule ??= new FraudRule { Id = Guid.NewGuid(), TenantId = TenantContext.TenantId, CreatedAt = DateTimeOffset.UtcNow };
        if (DbContext.Entry(rule).State == EntityState.Detached) DbContext.FraudRules.Add(rule);
        rule.Name = request.Name.Trim(); rule.RuleType = request.RuleType.Trim(); rule.ConfigurationJson = request.ConfigurationJson;
        rule.ScoreImpact = request.ScoreImpact; rule.Action = request.Action; rule.IsEnabled = request.IsEnabled; rule.UpdatedAt = DateTimeOffset.UtcNow;
        await DbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(TenantContext.TenantId, userId, "User", "fraud_rule.updated", "FraudRule", rule.Id.ToString(), null, Guid.NewGuid().ToString("N")), cancellationToken);
        return ToResponse(rule);
    }

    public async Task DeleteRuleAsync(Guid userId, Guid ruleId, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var rule = await DbContext.FraudRules.SingleOrDefaultAsync(x => x.Id == ruleId, cancellationToken) ?? throw new InvalidOperationException("Fraud rule was not found.");
        DbContext.FraudRules.Remove(rule); await DbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<FraudSummaryResponse> SummaryAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        var entries = await DbContext.CompetitionEntries.Where(x => x.CompetitionId == competitionId).ToListAsync(cancellationToken);
        var signals = await DbContext.EntryRiskSignals.Where(x => entries.Select(e => e.Id).Contains(x.EntryId)).GroupBy(x => x.SignalType)
            .Select(x => new FraudSignalSummaryResponse(x.Key, x.LongCount(), x.Sum(s => s.ScoreImpact))).ToListAsync(cancellationToken);
        return new FraudSummaryResponse(entries.LongCount(x => x.RiskLevel == RiskLevel.Low), entries.LongCount(x => x.RiskLevel == RiskLevel.Medium),
            entries.LongCount(x => x.RiskLevel == RiskLevel.High), entries.LongCount(x => x.RiskLevel == RiskLevel.Blocked), signals);
    }

    private static RuleDefinition ToDefinition(FraudRule rule)
    {
        using var doc = JsonDocument.Parse(rule.ConfigurationJson);
        return new RuleDefinition(rule.Id, rule.RuleType, rule.ScoreImpact, rule.Action, doc.RootElement.Clone());
    }
    private static int Threshold(IReadOnlyDictionary<string, RuleDefinition> rules, string type, string property, int fallback) =>
        rules.TryGetValue(type, out var rule) && rule.Configuration.TryGetProperty(property, out var value) && value.TryGetInt32(out var parsed) ? parsed : fallback;
    private static string[] Values(IReadOnlyDictionary<string, RuleDefinition> rules, string type, string property, string[] fallback) =>
        rules.TryGetValue(type, out var rule) && rule.Configuration.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(x => x.GetString()).Where(x => x is not null).Cast<string>().ToArray() : fallback;
    private static FraudRuleResponse ToResponse(FraudRule x) => new(x.Id, x.Name, x.RuleType, x.ConfigurationJson, x.ScoreImpact, x.Action, x.IsEnabled);
}
