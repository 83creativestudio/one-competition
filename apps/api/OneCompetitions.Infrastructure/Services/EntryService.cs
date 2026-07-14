using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneCompetitions.Application.Auditing;
using OneCompetitions.Application.Entries;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Contracts.Entries;
using OneCompetitions.Domain.Competitions;
using OneCompetitions.Domain.Consents;
using OneCompetitions.Domain.Entries;
using OneCompetitions.Domain.Fraud;
using OneCompetitions.Domain.Participants;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class EntryService(
    AppDbContext dbContext,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    IAuditLogger auditLogger)
    : TenantScopedServiceBase(dbContext, tenantContext, userManager), IEntryService
{
    public async Task<EntryResponse> SubmitAsync(string competitionSlug, SubmitEntryRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        if (!TenantContext.IsResolved)
        {
            throw new UnauthorizedAccessException("Tenant context is required.");
        }

        var competition = await DbContext.Competitions.SingleOrDefaultAsync(x => x.Slug == Normalize(competitionSlug), cancellationToken)
            ?? throw new InvalidOperationException("Competition was not found.");

        var now = DateTimeOffset.UtcNow;
        if (competition.Status != CompetitionStatus.Live || now < competition.StartsAt || now > competition.EndsAt)
        {
            throw new InvalidOperationException("Competition is not open for entries.");
        }

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await DbContext.CompetitionEntries
                .SingleOrDefaultAsync(x => x.CompetitionId == competition.Id && x.DeviceFingerprintHash == Hash(request.IdempotencyKey), cancellationToken);
            if (existing is not null)
            {
                return ToResponse(existing);
            }
        }

        var fields = await DbContext.CompetitionFields.Where(x => x.CompetitionId == competition.Id).ToListAsync(cancellationToken);
        foreach (var required in fields.Where(x => x.IsRequired))
        {
            if (!request.Answers.Any(x => string.Equals(x.FieldKey, required.FieldKey, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(x.Value)))
            {
                throw new InvalidOperationException($"Field '{required.FieldKey}' is required.");
            }
        }

        var requiredConsents = await DbContext.ConsentDefinitions
            .Where(x => x.CompetitionId == competition.Id && x.IsRequired)
            .ToListAsync(cancellationToken);
        foreach (var consent in requiredConsents)
        {
            if (!request.Consents.Any(x => x.ConsentDefinitionId == consent.Id && x.Accepted))
            {
                throw new InvalidOperationException("Required consent has not been accepted.");
            }
        }

        await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
        var email = Normalize(request.Email);
        var phone = Normalize(request.Phone);
        var participant = await DbContext.Participants.SingleOrDefaultAsync(x =>
                (!string.IsNullOrEmpty(email) && x.PrimaryEmail == email)
                || (!string.IsNullOrEmpty(phone) && x.PrimaryPhone == phone),
            cancellationToken);

        if (participant is null)
        {
            participant = new Participant
            {
                Id = Guid.NewGuid(),
                TenantId = TenantContext.TenantId,
                PrimaryEmail = email,
                PrimaryPhone = phone,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PreferredLanguage = string.IsNullOrWhiteSpace(request.PreferredLanguage) ? "en" : request.PreferredLanguage.Trim().ToLowerInvariant(),
                CreatedAt = now,
                UpdatedAt = now
            };
            DbContext.Participants.Add(participant);
        }

        var riskScore = 0;
        if (!string.IsNullOrEmpty(email) && await DbContext.CompetitionEntries.AnyAsync(x => x.CompetitionId == competition.Id && DbContext.Participants.Any(p => p.Id == x.ParticipantId && p.PrimaryEmail == email), cancellationToken))
        {
            riskScore += 40;
        }

        if (!string.IsNullOrEmpty(phone) && await DbContext.CompetitionEntries.AnyAsync(x => x.CompetitionId == competition.Id && DbContext.Participants.Any(p => p.Id == x.ParticipantId && p.PrimaryPhone == phone), cancellationToken))
        {
            riskScore += 40;
        }

        var entry = new CompetitionEntry
        {
            Id = Guid.NewGuid(),
            TenantId = TenantContext.TenantId,
            CompetitionId = competition.Id,
            ParticipantId = participant.Id,
            EntryReference = await NextReferenceAsync(cancellationToken),
            Status = competition.RequiresManualApproval || riskScore > 0 ? CompetitionEntryStatus.UnderReview : CompetitionEntryStatus.Approved,
            EligibilityStatus = competition.RequiresManualApproval || riskScore > 0 ? EligibilityStatus.PendingReview : EligibilityStatus.Eligible,
            RiskScore = riskScore,
            RiskLevel = riskScore >= 80 ? RiskLevel.High : riskScore >= 40 ? RiskLevel.Medium : RiskLevel.Low,
            EntrySourceId = request.CampaignSourceId,
            SubmittedAt = now,
            IpHash = HashNullable(ipAddress),
            UserAgentHash = HashNullable(userAgent),
            DeviceFingerprintHash = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : Hash(request.IdempotencyKey),
            CreatedAt = now,
            UpdatedAt = now
        };
        DbContext.CompetitionEntries.Add(entry);

        foreach (var answer in request.Answers)
        {
            var field = fields.SingleOrDefault(x => string.Equals(x.FieldKey, answer.FieldKey, StringComparison.OrdinalIgnoreCase));
            if (field is null)
            {
                continue;
            }

            DbContext.EntryAnswers.Add(new EntryAnswer
            {
                Id = Guid.NewGuid(),
                EntryId = entry.Id,
                CompetitionFieldId = field.Id,
                StringValue = answer.Value,
                CreatedAt = now
            });
        }

        foreach (var consent in request.Consents)
        {
            var definition = await DbContext.ConsentDefinitions.SingleOrDefaultAsync(x => x.Id == consent.ConsentDefinitionId, cancellationToken);
            if (definition is null)
            {
                continue;
            }

            DbContext.ParticipantConsents.Add(new ParticipantConsent
            {
                Id = Guid.NewGuid(),
                ParticipantId = participant.Id,
                EntryId = entry.Id,
                ConsentDefinitionId = consent.ConsentDefinitionId,
                Accepted = consent.Accepted,
                ConsentTextHash = Hash(definition.Text),
                IpHash = entry.IpHash,
                UserAgentHash = entry.UserAgentHash,
                AcceptedAt = now,
                CreatedAt = now
            });
        }

        if (riskScore > 0)
        {
            DbContext.EntryRiskSignals.Add(new EntryRiskSignal
            {
                Id = Guid.NewGuid(),
                EntryId = entry.Id,
                SignalType = "DuplicateContact",
                ScoreImpact = riskScore,
                Description = "Duplicate email or phone detected for this competition.",
                CreatedAt = now
            });
        }

        DbContext.AnalyticsEvents.Add(new Domain.Analytics.AnalyticsEvent
        {
            Id = Guid.NewGuid(),
            TenantId = TenantContext.TenantId,
            CompetitionId = competition.Id,
            EventType = "EntrySubmitted",
            CampaignSourceId = request.CampaignSourceId,
            OccurredAt = now
        });

        await DbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(TenantContext.TenantId, null, "Participant", "entry.submitted", "CompetitionEntry", entry.Id.ToString(), $"competition:{competition.Id}", Guid.NewGuid().ToString("N")), cancellationToken);
        return ToResponse(entry);
    }

    public async Task<IReadOnlyList<EntryResponse>> ListAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        return await DbContext.CompetitionEntries.Where(x => x.CompetitionId == competitionId).OrderByDescending(x => x.CreatedAt).Take(200).Select(x => ToResponse(x)).ToListAsync(cancellationToken);
    }

    public async Task<EntryResponse?> GetAsync(Guid userId, Guid competitionId, Guid entryId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        return await DbContext.CompetitionEntries.Where(x => x.CompetitionId == competitionId && x.Id == entryId).Select(x => ToResponse(x)).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<EntryResponse> ReviewAsync(Guid userId, Guid competitionId, Guid entryId, EntryReviewRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var entry = await DbContext.CompetitionEntries.SingleOrDefaultAsync(x => x.CompetitionId == competitionId && x.Id == entryId, cancellationToken)
            ?? throw new InvalidOperationException("Entry was not found.");

        if (!Enum.TryParse<EntryReviewDecision>(request.Decision, true, out var decision))
        {
            throw new InvalidOperationException("Review decision is not valid.");
        }

        entry.Status = decision switch
        {
            EntryReviewDecision.Approve or EntryReviewDecision.MarkTrusted => CompetitionEntryStatus.Approved,
            EntryReviewDecision.MarkDuplicate => CompetitionEntryStatus.Duplicate,
            EntryReviewDecision.Disqualify => CompetitionEntryStatus.Disqualified,
            EntryReviewDecision.Reject => CompetitionEntryStatus.Rejected,
            _ => CompetitionEntryStatus.UnderReview
        };
        entry.EligibilityStatus = entry.Status == CompetitionEntryStatus.Approved ? EligibilityStatus.Eligible : EligibilityStatus.Ineligible;
        entry.ApprovedAt = entry.Status == CompetitionEntryStatus.Approved ? DateTimeOffset.UtcNow : entry.ApprovedAt;
        entry.RejectedAt = entry.Status is CompetitionEntryStatus.Rejected or CompetitionEntryStatus.Disqualified or CompetitionEntryStatus.Duplicate ? DateTimeOffset.UtcNow : entry.RejectedAt;
        entry.RejectedReason = request.Notes;
        entry.UpdatedAt = DateTimeOffset.UtcNow;

        DbContext.EntryReviews.Add(new EntryReview { Id = Guid.NewGuid(), EntryId = entry.Id, ReviewerUserId = userId, Decision = decision, Notes = request.Notes, CreatedAt = DateTimeOffset.UtcNow });
        await DbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(TenantContext.TenantId, userId, "User", "entry.reviewed", "CompetitionEntry", entry.Id.ToString(), $"competition:{competitionId}", Guid.NewGuid().ToString("N")), cancellationToken);
        return ToResponse(entry);
    }

    public async Task<EntryResponse?> GetPublicStatusAsync(string reference, CancellationToken cancellationToken)
    {
        if (!TenantContext.IsResolved)
        {
            return null;
        }

        return await DbContext.CompetitionEntries.Where(x => x.EntryReference == reference).Select(x => ToResponse(x)).SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<string> NextReferenceAsync(CancellationToken cancellationToken)
    {
        var count = await DbContext.CompetitionEntries.CountAsync(cancellationToken) + 1;
        return $"ENT-{DateTimeOffset.UtcNow:yyyy}-{count:000000}";
    }

    private static string? HashNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : Hash(value);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
    private static EntryResponse ToResponse(CompetitionEntry entry) => new(entry.Id, entry.CompetitionId, entry.EntryReference, entry.Status.ToString(), entry.EligibilityStatus.ToString(), entry.RiskScore, entry.RiskLevel.ToString(), entry.SubmittedAt, entry.RejectedReason);
}
