using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneCompetitions.Application.Auditing;
using OneCompetitions.Application.Entries;
using OneCompetitions.Application.Fraud;
using OneCompetitions.Application.Notifications;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Application.Webhooks;
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
    IAuditLogger auditLogger,
    INotificationQueue notifications,
    IWebhookService webhooks,
    ICaptchaProvider captcha,
    SecretProtector protector,
    IFraudService fraud)
    : TenantScopedServiceBase(dbContext, tenantContext, userManager), IEntryService
{
    public async Task<EntryResponse> SubmitAsync(string competitionSlug, SubmitEntryRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        if (!TenantContext.IsResolved)
        {
            throw new UnauthorizedAccessException("Tenant context is required.");
        }

        if (!await captcha.ValidateAsync(request.CaptchaToken, ipAddress, cancellationToken))
            throw new UnauthorizedAccessException("Anti-bot verification failed.");

        var competition = await DbContext.Competitions.SingleOrDefaultAsync(x => x.Slug == Normalize(competitionSlug), cancellationToken)
            ?? throw new InvalidOperationException("Competition was not found.");

        var now = DateTimeOffset.UtcNow;
        if (competition.Status != CompetitionStatus.Live || now < competition.StartsAt || now > competition.EndsAt)
        {
            throw new InvalidOperationException("Competition is not open for entries.");
        }

        if (competition.RequiresEmailVerification && string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("Email is required for this competition.");
        if (competition.RequiresPhoneVerification && string.IsNullOrWhiteSpace(request.Phone))
            throw new InvalidOperationException("Phone number is required for this competition.");
        ValidateEligibility(competition, request, now);
        if (competition.EntryLimit is not null && await DbContext.CompetitionEntries.CountAsync(x => x.CompetitionId == competition.Id, cancellationToken) >= competition.EntryLimit)
            throw new InvalidOperationException("The competition entry limit has been reached.");

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await DbContext.CompetitionEntries
                .SingleOrDefaultAsync(x => x.CompetitionId == competition.Id && x.IdempotencyKeyHash == Hash(request.IdempotencyKey), cancellationToken);
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
                DateOfBirth = request.DateOfBirth,
                CountryCode = NormalizeCountry(request.CountryCode),
                PreferredLanguage = string.IsNullOrWhiteSpace(request.PreferredLanguage) ? "en" : request.PreferredLanguage.Trim().ToLowerInvariant(),
                CreatedAt = now,
                UpdatedAt = now
            };
            DbContext.Participants.Add(participant);
        }
        else
        {
            participant.DateOfBirth ??= request.DateOfBirth;
            participant.CountryCode ??= NormalizeCountry(request.CountryCode);
            participant.UpdatedAt = now;
        }

        if (await DbContext.CompetitionEntries.CountAsync(x => x.CompetitionId == competition.Id && x.ParticipantId == participant.Id, cancellationToken) >= competition.PerParticipantEntryLimit)
            throw new InvalidOperationException("The participant entry limit has been reached.");

        var ipHash = HashNullable(ipAddress);
        var deviceHash = HashNullable(request.DeviceFingerprint);
        var evaluation = await fraud.EvaluateAsync(new FraudEvaluationRequest(competition.Id, email, phone, ipHash, deviceHash, request.FormStartedAt, now), cancellationToken);
        var riskScore = evaluation.Score;

        var requiresVerification = competition.RequiresEmailVerification || competition.RequiresPhoneVerification;
        var postVerificationStatus = competition.RequiresManualApproval || riskScore > 0 ? CompetitionEntryStatus.UnderReview : CompetitionEntryStatus.Approved;

        var entry = new CompetitionEntry
        {
            Id = Guid.NewGuid(),
            TenantId = TenantContext.TenantId,
            CompetitionId = competition.Id,
            ParticipantId = participant.Id,
            EntryReference = await NextReferenceAsync(cancellationToken),
            Status = evaluation.ShouldBlock ? CompetitionEntryStatus.Rejected : competition.RequiresEmailVerification ? CompetitionEntryStatus.EmailVerificationPending
                : competition.RequiresPhoneVerification ? CompetitionEntryStatus.PhoneVerificationPending : postVerificationStatus,
            EligibilityStatus = evaluation.ShouldBlock ? EligibilityStatus.Ineligible : requiresVerification || competition.RequiresManualApproval || riskScore > 0 ? EligibilityStatus.PendingReview : EligibilityStatus.Eligible,
            RiskScore = riskScore,
            RiskLevel = evaluation.ShouldBlock ? RiskLevel.Blocked : riskScore >= 80 ? RiskLevel.High : riskScore >= 40 ? RiskLevel.Medium : RiskLevel.Low,
            EntrySourceId = request.CampaignSourceId,
            SubmittedAt = now,
            IpHash = ipHash,
            UserAgentHash = HashNullable(userAgent),
            DeviceFingerprintHash = deviceHash,
            IdempotencyKeyHash = HashNullable(request.IdempotencyKey),
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

            var entity = new EntryAnswer
            {
                Id = Guid.NewGuid(),
                EntryId = entry.Id,
                CompetitionFieldId = field.Id,
                CreatedAt = now
            };
            if (field.IsSensitive) entity.EncryptedValue = protector.Protect(answer.Value ?? string.Empty);
            else entity.StringValue = answer.Value;
            DbContext.EntryAnswers.Add(entity);
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

        foreach (var signal in evaluation.Signals)
        {
            DbContext.EntryRiskSignals.Add(new EntryRiskSignal
            {
                Id = Guid.NewGuid(),
                EntryId = entry.Id,
                RuleId = signal.RuleId,
                SignalType = signal.SignalType,
                ScoreImpact = signal.ScoreImpact,
                Description = signal.Description,
                EvidenceJson = signal.EvidenceJson,
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

        string? emailCode = null;
        string? phoneCode = null;
        if (competition.RequiresEmailVerification && !evaluation.ShouldBlock)
        {
            emailCode = GenerateCode();
            AddVerification(entry, "Email", emailCode, now);
        }
        if (competition.RequiresPhoneVerification && !evaluation.ShouldBlock)
        {
            phoneCode = GenerateCode();
            AddVerification(entry, "Phone", phoneCode, now);
        }

        await DbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(TenantContext.TenantId, null, "Participant", "entry.submitted", "CompetitionEntry", entry.Id.ToString(), $"competition:{competition.Id}", Guid.NewGuid().ToString("N")), cancellationToken);
        if (emailCode is not null && !string.IsNullOrWhiteSpace(participant.PrimaryEmail))
        {
            await notifications.QueueEmailAsync(TenantContext.TenantId, competition.Id,
                new EmailMessage(participant.PrimaryEmail, "Verify your competition entry", $"Your verification code is {emailCode}. It expires in 15 minutes."), cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(participant.PrimaryEmail))
        {
            await notifications.QueueEmailAsync(TenantContext.TenantId, competition.Id,
                new EmailMessage(participant.PrimaryEmail, "Competition entry received", $"Your entry reference is {entry.EntryReference}."), cancellationToken);
        }
        if (phoneCode is not null && !string.IsNullOrWhiteSpace(participant.PrimaryPhone))
            await notifications.QueueSmsAsync(TenantContext.TenantId, competition.Id, new SmsMessage(participant.PrimaryPhone, $"ONE. Competitions verification code: {phoneCode}"), cancellationToken);
        await webhooks.QueueEventAsync(TenantContext.TenantId, "entry.submitted", new { entry.EntryReference, entry.CompetitionId, entry.SubmittedAt }, cancellationToken);
        return ToResponse(entry);
    }

    public async Task<IReadOnlyList<EntryResponse>> ListAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        if (DbContext.Database.IsSqlite())
            return (await DbContext.CompetitionEntries.Where(x => x.CompetitionId == competitionId).ToListAsync(cancellationToken)).OrderByDescending(x => x.CreatedAt).Take(200).Select(ToResponse).ToList();
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

    public async Task<EntryResponse> VerifyAsync(string reference, string channel, VerifyEntryRequest request, CancellationToken cancellationToken)
    {
        if (!TenantContext.IsResolved) throw new UnauthorizedAccessException("Tenant context is required.");
        var normalizedChannel = channel.Equals("email", StringComparison.OrdinalIgnoreCase) ? "Email"
            : channel.Equals("phone", StringComparison.OrdinalIgnoreCase) ? "Phone"
            : throw new InvalidOperationException("Verification channel is invalid.");
        var entry = await DbContext.CompetitionEntries.SingleOrDefaultAsync(x => x.EntryReference == reference, cancellationToken)
            ?? throw new InvalidOperationException("Entry was not found.");
        var verification = await DbContext.EntryVerifications.SingleOrDefaultAsync(x => x.EntryId == entry.Id && x.Channel == normalizedChannel, cancellationToken)
            ?? throw new InvalidOperationException("Verification request was not found.");
        if (verification.VerifiedAt is not null) return ToResponse(entry);
        if (verification.ExpiresAt <= DateTimeOffset.UtcNow || verification.AttemptCount >= 5)
            throw new InvalidOperationException("Verification code is expired or locked.");
        verification.AttemptCount += 1;
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(verification.TokenHash), Encoding.UTF8.GetBytes(Hash(request.Code.Trim()))))
        {
            await DbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Verification code is invalid.");
        }

        verification.VerifiedAt = DateTimeOffset.UtcNow;
        var competition = await DbContext.Competitions.SingleAsync(x => x.Id == entry.CompetitionId, cancellationToken);
        var pendingOther = await DbContext.EntryVerifications.AnyAsync(x => x.EntryId == entry.Id && x.Id != verification.Id && x.VerifiedAt == null, cancellationToken);
        if (!pendingOther)
        {
            entry.VerifiedAt = DateTimeOffset.UtcNow;
            entry.Status = competition.RequiresManualApproval || entry.RiskScore > 0 ? CompetitionEntryStatus.UnderReview : CompetitionEntryStatus.Approved;
            entry.EligibilityStatus = entry.Status == CompetitionEntryStatus.Approved ? EligibilityStatus.Eligible : EligibilityStatus.PendingReview;
            entry.ApprovedAt = entry.Status == CompetitionEntryStatus.Approved ? DateTimeOffset.UtcNow : null;
        }
        else if (normalizedChannel == "Email") entry.Status = CompetitionEntryStatus.PhoneVerificationPending;
        entry.UpdatedAt = DateTimeOffset.UtcNow;
        await DbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(TenantContext.TenantId, null, "Participant", "entry.verified", "CompetitionEntry", entry.Id.ToString(), $"channel:{normalizedChannel}", Guid.NewGuid().ToString("N")), cancellationToken);
        if (!pendingOther) await webhooks.QueueEventAsync(TenantContext.TenantId, "entry.verified", new { entry.EntryReference, entry.CompetitionId, entry.VerifiedAt }, cancellationToken);
        return ToResponse(entry);
    }

    private async Task<string> NextReferenceAsync(CancellationToken cancellationToken)
    {
        var count = await DbContext.CompetitionEntries.CountAsync(cancellationToken) + 1;
        return $"ENT-{DateTimeOffset.UtcNow:yyyy}-{count:000000}";
    }

    private static string? HashNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : Hash(value);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
    private static string? NormalizeCountry(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    private static string GenerateCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    private void AddVerification(CompetitionEntry entry, string channel, string code, DateTimeOffset now) => DbContext.EntryVerifications.Add(new EntryVerification
    {
        Id = Guid.NewGuid(), TenantId = TenantContext.TenantId, EntryId = entry.Id, Channel = channel,
        TokenHash = Hash(code), ExpiresAt = now.AddMinutes(15), CreatedAt = now
    });

    private static void ValidateEligibility(Competition competition, SubmitEntryRequest request, DateTimeOffset now)
    {
        if (competition.MinimumAge is not null)
        {
            if (request.DateOfBirth is null) throw new InvalidOperationException("Date of birth is required for this competition.");
            var today = DateOnly.FromDateTime(now.UtcDateTime);
            var age = today.Year - request.DateOfBirth.Value.Year;
            if (request.DateOfBirth.Value > today.AddYears(-age)) age--;
            if (age < competition.MinimumAge) throw new InvalidOperationException("Participant does not meet the minimum age requirement.");
        }
        var countries = JsonSerializer.Deserialize<string[]>(competition.AllowedCountryCodesJson) ?? [];
        if (countries.Length > 0 && (string.IsNullOrWhiteSpace(request.CountryCode) || !countries.Contains(request.CountryCode.Trim(), StringComparer.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Participant country is not eligible for this competition.");
    }
    private static EntryResponse ToResponse(CompetitionEntry entry) => new(entry.Id, entry.CompetitionId, entry.EntryReference, entry.Status.ToString(), entry.EligibilityStatus.ToString(), entry.RiskScore, entry.RiskLevel.ToString(), entry.SubmittedAt, entry.RejectedReason);
}
