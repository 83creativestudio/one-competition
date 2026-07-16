using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneCompetitions.Application.Auditing;
using OneCompetitions.Application.Draws;
using OneCompetitions.Application.Locking;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Application.Webhooks;
using OneCompetitions.Contracts.Draws;
using OneCompetitions.Domain.Competitions;
using OneCompetitions.Domain.Draws;
using OneCompetitions.Domain.Entries;
using OneCompetitions.Domain.Winners;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class DrawService(
    AppDbContext dbContext,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    IAuditLogger auditLogger,
    IDistributedLockProvider distributedLocks,
    IWebhookService webhooks)
    : TenantScopedServiceBase(dbContext, tenantContext, userManager), IDrawService
{
    public async Task<IReadOnlyList<DrawResponse>> ListAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        if (DbContext.Database.IsSqlite())
            return (await DbContext.Draws.Where(x => x.CompetitionId == competitionId).Take(100).ToListAsync(cancellationToken)).OrderByDescending(x => x.CreatedAt).Select(ToResponse).ToList();
        return await DbContext.Draws.Where(x => x.CompetitionId == competitionId).OrderByDescending(x => x.CreatedAt).Select(x => ToResponse(x)).ToListAsync(cancellationToken);
    }

    public async Task<DrawResponse> PrepareAsync(Guid userId, Guid competitionId, PrepareDrawRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var competition = await DbContext.Competitions.SingleOrDefaultAsync(x => x.Id == competitionId, cancellationToken)
            ?? throw new InvalidOperationException("Competition was not found.");
        if (competition.Status != CompetitionStatus.Closed)
        {
            throw new InvalidOperationException("Only closed competitions can prepare a draw.");
        }

        var entries = await DbContext.CompetitionEntries
            .Where(x => x.CompetitionId == competitionId)
            .OrderBy(x => x.EntryReference)
            .ToListAsync(cancellationToken);

        var eligible = entries.Where(x => x.Status == CompetitionEntryStatus.Approved && x.EligibilityStatus == EligibilityStatus.Eligible).ToList();
        if (eligible.Count < request.WinnerCount)
        {
            throw new InvalidOperationException("Not enough eligible entries for the requested winners.");
        }

        var draw = new Draw
        {
            Id = Guid.NewGuid(),
            TenantId = TenantContext.TenantId,
            CompetitionId = competitionId,
            Status = competition.RequireDrawApproval ? DrawStatus.AwaitingApproval : DrawStatus.Approved,
            DrawReference = $"DRAW-{DateTimeOffset.UtcNow:yyyyMMdd}-{RandomNumberGenerator.GetInt32(100000, 999999)}",
            RequestedWinnerCount = request.WinnerCount,
            RequestedReserveCount = request.ReserveCount,
            EligibleEntryCount = eligible.Count,
            ExcludedEntryCount = entries.Count - eligible.Count,
            PreparedByUserId = userId,
            PreparedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var entry in entries)
        {
            var included = eligible.Any(x => x.Id == entry.Id);
            var snapshot = new DrawEntrySnapshot
            {
                Id = Guid.NewGuid(),
                DrawId = draw.Id,
                EntryId = entry.Id,
                EntryReference = entry.EntryReference,
                ParticipantReferenceHash = Hash(entry.ParticipantId.ToString()),
                Included = included,
                ExclusionReason = included ? null : entry.Status.ToString(),
                SnapshotHash = Hash($"{entry.Id}:{entry.EntryReference}:{entry.ParticipantId}:{included}"),
                CreatedAt = DateTimeOffset.UtcNow
            };
            DbContext.DrawEntrySnapshots.Add(snapshot);
        }

        draw.EntryPoolHash = Hash(string.Join("|", eligible.Select(x => $"{x.Id}:{x.EntryReference}")));
        draw.ConfigurationHash = Hash($"{competition.Id}:{request.WinnerCount}:{request.ReserveCount}:{competition.NumberOfWinners}:{competition.NumberOfReserveWinners}");
        DbContext.Draws.Add(draw);
        await DbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(TenantContext.TenantId, userId, "User", "draw.prepared", "Draw", draw.Id.ToString(), $"competition:{competitionId}", Guid.NewGuid().ToString("N")), cancellationToken);
        return ToResponse(draw);
    }

    public async Task<DrawResponse> ApproveAsync(Guid userId, Guid competitionId, Guid drawId, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var draw = await DbContext.Draws.SingleOrDefaultAsync(x => x.Id == drawId && x.CompetitionId == competitionId, cancellationToken)
            ?? throw new InvalidOperationException("Draw was not found.");
        if (draw.PreparedByUserId == userId)
        {
            throw new InvalidOperationException("The draw preparer cannot approve the same draw.");
        }

        draw.Status = DrawStatus.Approved;
        draw.ApprovedByUserId = userId;
        draw.ApprovedAt = DateTimeOffset.UtcNow;
        await DbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(TenantContext.TenantId, userId, "User", "draw.approved", "Draw", draw.Id.ToString(), $"competition:{competitionId}", Guid.NewGuid().ToString("N")), cancellationToken);
        return ToResponse(draw);
    }

    public async Task<DrawResponse> ExecuteAsync(Guid userId, Guid competitionId, Guid drawId, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        await using var drawLock = await distributedLocks.TryAcquireAsync($"draw:{drawId:N}", TimeSpan.FromMinutes(2), cancellationToken);
        if (drawLock is null)
        {
            throw new InvalidOperationException("Draw execution is already in progress.");
        }
        await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
        var draw = await DbContext.Draws.SingleOrDefaultAsync(x => x.Id == drawId && x.CompetitionId == competitionId, cancellationToken)
            ?? throw new InvalidOperationException("Draw was not found.");
        if (draw.Status != DrawStatus.Approved)
        {
            throw new InvalidOperationException("Draw must be approved before execution.");
        }

        if (await DbContext.DrawResults.AnyAsync(x => x.DrawId == draw.Id, cancellationToken))
        {
            throw new InvalidOperationException("Draw has already been executed.");
        }

        var snapshots = await DbContext.DrawEntrySnapshots.Where(x => x.DrawId == draw.Id && x.Included).OrderBy(x => x.EntryReference).ToListAsync(cancellationToken);
        var currentPoolHash = Hash(string.Join("|", snapshots.Select(x => $"{x.EntryId}:{x.EntryReference}")));
        if (currentPoolHash != draw.EntryPoolHash)
        {
            throw new InvalidOperationException("Draw pool hash validation failed.");
        }

        draw.Status = DrawStatus.Executing;
        await DbContext.SaveChangesAsync(cancellationToken);

        var selected = SelectWithoutReplacement(snapshots, draw.RequestedWinnerCount + draw.RequestedReserveCount);
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < selected.Count; i++)
        {
            var resultType = i < draw.RequestedWinnerCount ? DrawResultType.Winner : DrawResultType.ReserveWinner;
            var position = resultType == DrawResultType.Winner ? i + 1 : i - draw.RequestedWinnerCount + 1;
            var result = new DrawResult
            {
                Id = Guid.NewGuid(),
                DrawId = draw.Id,
                EntryId = selected[i].EntryId,
                ResultType = resultType,
                Position = position,
                SelectedAt = now,
                SelectionProofHash = Hash($"{draw.EntryPoolHash}:{selected[i].EntryId}:{resultType}:{position}:{now:O}"),
                CreatedAt = now
            };
            DbContext.DrawResults.Add(result);
            DbContext.WinnerClaims.Add(new WinnerClaim
            {
                Id = Guid.NewGuid(),
                DrawResultId = result.Id,
                EntryId = result.EntryId,
                Status = resultType == DrawResultType.Winner ? WinnerClaimStatus.ContactPending : WinnerClaimStatus.Selected,
                CreatedAt = now,
                UpdatedAt = now
            });

            var entry = await DbContext.CompetitionEntries.SingleAsync(x => x.Id == result.EntryId, cancellationToken);
            entry.Status = resultType == DrawResultType.Winner ? CompetitionEntryStatus.Winner : CompetitionEntryStatus.ReserveWinner;
            entry.UpdatedAt = now;
        }

        draw.Status = DrawStatus.Completed;
        draw.ExecutedByUserId = userId;
        draw.ExecutedAt = now;
        await DbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(TenantContext.TenantId, userId, "User", "draw.executed", "Draw", draw.Id.ToString(), $"competition:{competitionId}", Guid.NewGuid().ToString("N")), cancellationToken);
        await webhooks.QueueEventAsync(TenantContext.TenantId, "draw.completed", new { draw.Id, draw.DrawReference, draw.CompetitionId, draw.ExecutedAt }, cancellationToken);
        return ToResponse(draw);
    }

    public async Task<IReadOnlyList<DrawResultResponse>> ResultsAsync(Guid userId, Guid competitionId, Guid drawId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        var query =
            from result in DbContext.DrawResults
            join entry in DbContext.CompetitionEntries on result.EntryId equals entry.Id
            join draw in DbContext.Draws on result.DrawId equals draw.Id
            where result.DrawId == drawId && draw.CompetitionId == competitionId
            orderby result.ResultType, result.Position
            select new DrawResultResponse(result.Id, result.DrawId, result.EntryId, result.ResultType.ToString(), result.Position, entry.EntryReference, result.SelectedAt);
        return await query.ToListAsync(cancellationToken);
    }

    private static List<DrawEntrySnapshot> SelectWithoutReplacement(List<DrawEntrySnapshot> snapshots, int count)
    {
        var pool = snapshots.ToList();
        var selected = new List<DrawEntrySnapshot>();
        while (selected.Count < count && pool.Count > 0)
        {
            var index = RandomNumberGenerator.GetInt32(pool.Count);
            selected.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return selected;
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static DrawResponse ToResponse(Draw draw) => new(draw.Id, draw.CompetitionId, draw.Status.ToString(), draw.DrawReference, draw.RequestedWinnerCount, draw.RequestedReserveCount, draw.EligibleEntryCount, draw.ExcludedEntryCount, draw.EntryPoolHash, draw.ConfigurationHash, draw.PreparedAt, draw.ApprovedAt, draw.ExecutedAt);
}
