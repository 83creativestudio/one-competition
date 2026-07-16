using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneCompetitions.Application.Auditing;
using OneCompetitions.Application.Notifications;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Application.Winners;
using OneCompetitions.Contracts.Winners;
using OneCompetitions.Domain.Draws;
using OneCompetitions.Domain.Winners;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class WinnerService(
    AppDbContext dbContext,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    IAuditLogger auditLogger,
    INotificationQueue notifications)
    : TenantScopedServiceBase(dbContext, tenantContext, userManager), IWinnerService
{
    public async Task<IReadOnlyList<WinnerClaimResponse>> ListAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        var query =
            from claim in DbContext.WinnerClaims
            join result in DbContext.DrawResults on claim.DrawResultId equals result.Id
            join draw in DbContext.Draws on result.DrawId equals draw.Id
            where draw.CompetitionId == competitionId
            orderby result.ResultType, result.Position
            select ToResponse(claim);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<WinnerClaimResponse> ContactAsync(Guid userId, Guid competitionId, Guid winnerId, WinnerActionRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var claim = await FindAsync(competitionId, winnerId, cancellationToken);
        var participant = await (from entry in DbContext.CompetitionEntries
                                 join person in DbContext.Participants on entry.ParticipantId equals person.Id
                                 where entry.Id == claim.EntryId
                                 select person).SingleAsync(cancellationToken);
        var channel = string.IsNullOrWhiteSpace(request.Channel) ? "Manual" : request.Channel.Trim();
        string recipientMasked;
        string outcome;
        if (channel.Equals("Email", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(participant.PrimaryEmail)) throw new InvalidOperationException("The winner does not have an email address.");
            await notifications.QueueEmailAsync(TenantContext.TenantId, competitionId,
                new EmailMessage(participant.PrimaryEmail, "You have been selected as a competition winner", "Please contact the competition organiser to verify your eligibility and claim your prize."), cancellationToken);
            recipientMasked = MaskEmail(participant.PrimaryEmail);
            outcome = "Queued";
            channel = "Email";
        }
        else if (channel.Equals("Sms", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(participant.PrimaryPhone)) throw new InvalidOperationException("The winner does not have a phone number.");
            await notifications.QueueSmsAsync(TenantContext.TenantId, competitionId,
                new SmsMessage(participant.PrimaryPhone, "ONE. Competitions: you have been selected as a winner. Please contact the organiser to verify and claim your prize."), cancellationToken);
            recipientMasked = MaskPhone(participant.PrimaryPhone);
            outcome = "Queued";
            channel = "Sms";
        }
        else if (channel.Equals("Manual", StringComparison.OrdinalIgnoreCase))
        {
            recipientMasked = "recorded-outside-system";
            outcome = "Contacted";
            channel = "Manual";
        }
        else throw new InvalidOperationException("Contact channel must be Email, Sms, or Manual.");

        var now = DateTimeOffset.UtcNow;
        claim.Status = WinnerClaimStatus.Contacted;
        claim.FirstContactedAt ??= now;
        claim.LastContactedAt = now;
        claim.UpdatedAt = now;
        DbContext.WinnerContactAttempts.Add(new WinnerContactAttempt
        {
            Id = Guid.NewGuid(), WinnerClaimId = claim.Id, Channel = channel, RecipientMasked = recipientMasked,
            Outcome = outcome, Notes = request.Notes, AttemptedByUserId = userId, AttemptedAt = now
        });
        await DbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(TenantContext.TenantId, userId, "User", "winner.contacted", "WinnerClaim", claim.Id.ToString(), $"competition:{competitionId};channel:{channel}", Guid.NewGuid().ToString("N")), cancellationToken);
        return ToResponse(claim);
    }

    public Task<WinnerClaimResponse> AcceptAsync(Guid userId, Guid competitionId, Guid winnerId, CancellationToken cancellationToken)
    {
        return MutateAsync(userId, competitionId, winnerId, "winner.accepted", claim =>
        {
            claim.Status = WinnerClaimStatus.Accepted;
            claim.AcceptedAt = DateTimeOffset.UtcNow;
        }, cancellationToken);
    }

    public async Task<WinnerClaimResponse> DisqualifyAsync(Guid userId, Guid competitionId, Guid winnerId, WinnerActionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Notes))
        {
            throw new InvalidOperationException("Disqualification reason is required.");
        }

        await EnsureTenantManagerAsync(userId, cancellationToken);
        var claim = await FindAsync(competitionId, winnerId, cancellationToken);
        claim.Status = WinnerClaimStatus.Disqualified;
        claim.DisqualifiedAt = DateTimeOffset.UtcNow;
        claim.DisqualificationReason = request.Notes;
        claim.UpdatedAt = DateTimeOffset.UtcNow;

        var reserve =
            await (from reserveClaim in DbContext.WinnerClaims
                   join reserveResult in DbContext.DrawResults on reserveClaim.DrawResultId equals reserveResult.Id
                   join draw in DbContext.Draws on reserveResult.DrawId equals draw.Id
                   where draw.CompetitionId == competitionId
                         && reserveResult.ResultType == DrawResultType.ReserveWinner
                         && reserveClaim.Status == WinnerClaimStatus.Selected
                   orderby reserveResult.Position
                   select reserveClaim)
                .FirstOrDefaultAsync(cancellationToken);

        if (reserve is not null)
        {
            reserve.Status = WinnerClaimStatus.ContactPending;
            reserve.UpdatedAt = DateTimeOffset.UtcNow;
            claim.ReplacedByWinnerClaimId = reserve.Id;
        }

        await DbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(TenantContext.TenantId, userId, "User", "winner.disqualified", "WinnerClaim", claim.Id.ToString(), $"competition:{competitionId}", Guid.NewGuid().ToString("N")), cancellationToken);
        return ToResponse(claim);
    }

    public Task<WinnerClaimResponse> DeliverPrizeAsync(Guid userId, Guid competitionId, Guid winnerId, CancellationToken cancellationToken)
    {
        return MutateAsync(userId, competitionId, winnerId, "prize.delivered", claim =>
        {
            claim.Status = WinnerClaimStatus.PrizeDelivered;
            claim.PrizeDeliveredAt = DateTimeOffset.UtcNow;
        }, cancellationToken);
    }

    private async Task<WinnerClaimResponse> MutateAsync(Guid userId, Guid competitionId, Guid winnerId, string auditAction, Action<WinnerClaim> apply, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var claim = await FindAsync(competitionId, winnerId, cancellationToken);
        apply(claim);
        claim.UpdatedAt = DateTimeOffset.UtcNow;
        await DbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(TenantContext.TenantId, userId, "User", auditAction, "WinnerClaim", claim.Id.ToString(), $"competition:{competitionId}", Guid.NewGuid().ToString("N")), cancellationToken);
        return ToResponse(claim);
    }

    private async Task<WinnerClaim> FindAsync(Guid competitionId, Guid winnerId, CancellationToken cancellationToken)
    {
        var claim =
            await (from winner in DbContext.WinnerClaims
                   join result in DbContext.DrawResults on winner.DrawResultId equals result.Id
                   join draw in DbContext.Draws on result.DrawId equals draw.Id
                   where winner.Id == winnerId && draw.CompetitionId == competitionId
                   select winner)
                .SingleOrDefaultAsync(cancellationToken);
        return claim ?? throw new InvalidOperationException("Winner claim was not found.");
    }

    private static WinnerClaimResponse ToResponse(WinnerClaim claim) => new(claim.Id, claim.DrawResultId, claim.EntryId, claim.Status.ToString(), claim.FirstContactedAt, claim.AcceptedAt, claim.PrizeDeliveredAt, claim.DisqualificationReason);
    private static string MaskEmail(string value)
    {
        var parts = value.Split('@', 2);
        return parts.Length == 2 ? $"{parts[0][0]}***@{parts[1]}" : "***";
    }
    private static string MaskPhone(string value) => value.Length <= 4 ? "****" : $"***{value[^4..]}";
}
