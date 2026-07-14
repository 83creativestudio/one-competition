using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneCompetitions.Application.Auditing;
using OneCompetitions.Application.Exports;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Contracts.Exports;
using OneCompetitions.Domain.Exports;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class ExportService(
    AppDbContext dbContext,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    IAuditLogger auditLogger)
    : TenantScopedServiceBase(dbContext, tenantContext, userManager), IExportService
{
    public async Task<ExportJobResponse> CreateAsync(Guid userId, Guid competitionId, CreateExportRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        var job = new ExportJob
        {
            Id = Guid.NewGuid(),
            TenantId = TenantContext.TenantId,
            CompetitionId = competitionId,
            ExportType = request.ExportType,
            Format = request.Format,
            Status = "Completed",
            RequestedByUserId = userId,
            StorageKey = $"tenant/{TenantContext.TenantId}/exports/{Guid.NewGuid():N}.csv",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(14),
            CreatedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow
        };
        DbContext.ExportJobs.Add(job);
        await DbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(TenantContext.TenantId, userId, "User", "export.created", "ExportJob", job.Id.ToString(), $"competition:{competitionId}", Guid.NewGuid().ToString("N")), cancellationToken);
        return ToResponse(job);
    }

    public async Task<IReadOnlyList<ExportJobResponse>> ListAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        return await DbContext.ExportJobs.Where(x => x.CompetitionId == competitionId).OrderByDescending(x => x.CreatedAt).Select(x => ToResponse(x)).ToListAsync(cancellationToken);
    }

    private static ExportJobResponse ToResponse(ExportJob job) => new(job.Id, job.CompetitionId, job.ExportType, job.Format, job.Status, job.StorageKey, job.ExpiresAt, job.CreatedAt, job.CompletedAt);
}
