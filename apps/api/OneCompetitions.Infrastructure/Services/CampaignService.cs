using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneCompetitions.Application.Auditing;
using OneCompetitions.Application.Campaigns;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Contracts.Analytics;
using OneCompetitions.Contracts.Campaigns;
using OneCompetitions.Contracts.Qr;
using OneCompetitions.Domain.Campaigns;
using OneCompetitions.Domain.Entries;
using OneCompetitions.Domain.Qr;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class CampaignService(
    AppDbContext dbContext,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    IAuditLogger auditLogger)
    : TenantScopedServiceBase(dbContext, tenantContext, userManager), ICampaignService
{
    public async Task<IReadOnlyList<CampaignSourceResponse>> ListSourcesAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        return await DbContext.CampaignSources.Where(x => x.CompetitionId == competitionId).OrderBy(x => x.Name).Select(x => ToResponse(x)).ToListAsync(cancellationToken);
    }

    public async Task<CampaignSourceResponse> CreateSourceAsync(Guid userId, Guid competitionId, CreateCampaignSourceRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        if (!Enum.TryParse<CampaignSourceType>(request.SourceType, true, out var sourceType))
        {
            throw new InvalidOperationException("Campaign source type is not valid.");
        }

        var source = new CampaignSource
        {
            Id = Guid.NewGuid(),
            TenantId = TenantContext.TenantId,
            CompetitionId = competitionId,
            Name = request.Name.Trim(),
            SourceType = sourceType,
            Code = NormalizeCode(request.Code),
            UtmSource = request.UtmSource,
            UtmMedium = request.UtmMedium,
            UtmCampaign = request.UtmCampaign,
            UtmContent = request.UtmContent,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        DbContext.CampaignSources.Add(source);
        await DbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(TenantContext.TenantId, userId, "User", "campaign_source.created", "CampaignSource", source.Id.ToString(), $"competition:{competitionId}", Guid.NewGuid().ToString("N")), cancellationToken);
        return ToResponse(source);
    }

    public async Task<IReadOnlyList<QrCodeResponse>> ListQrCodesAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        return await DbContext.QrCodes.Where(x => x.CompetitionId == competitionId).OrderByDescending(x => x.CreatedAt).Select(x => ToResponse(x)).ToListAsync(cancellationToken);
    }

    public async Task<QrCodeResponse> CreateQrCodeAsync(Guid userId, Guid competitionId, CreateQrCodeRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        if (!await DbContext.CampaignSources.AnyAsync(x => x.Id == request.CampaignSourceId && x.CompetitionId == competitionId, cancellationToken))
        {
            throw new InvalidOperationException("Campaign source was not found.");
        }

        var qr = new QrCode
        {
            Id = Guid.NewGuid(),
            TenantId = TenantContext.TenantId,
            CompetitionId = competitionId,
            CampaignSourceId = request.CampaignSourceId,
            ShortCode = ShortCode(),
            DestinationUrl = request.DestinationUrl,
            StyleJson = string.IsNullOrWhiteSpace(request.StyleJson) ? "{}" : request.StyleJson,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        DbContext.QrCodes.Add(qr);
        await DbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(TenantContext.TenantId, userId, "User", "qr_code.created", "QrCode", qr.Id.ToString(), $"competition:{competitionId}", Guid.NewGuid().ToString("N")), cancellationToken);
        return ToResponse(qr);
    }

    public async Task<string> RecordQrScanAndGetRedirectAsync(string shortCode, string? ipAddress, string? userAgent, string? referrer, CancellationToken cancellationToken)
    {
        var qr = await DbContext.QrCodes.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.ShortCode == shortCode, cancellationToken)
            ?? throw new InvalidOperationException("QR code was not found.");

        if (qr.Status != QrCodeStatus.Active || (qr.ExpiresAt is not null && qr.ExpiresAt <= DateTimeOffset.UtcNow))
        {
            throw new InvalidOperationException("QR code is not active.");
        }

        var ipHash = HashNullable(ipAddress);
        var unique = ipHash is null || !await DbContext.QrScans.IgnoreQueryFilters().AnyAsync(x => x.QrCodeId == qr.Id && x.IpHash == ipHash, cancellationToken);
        qr.ScanCount += 1;
        qr.UniqueScanCount += unique ? 1 : 0;
        qr.UpdatedAt = DateTimeOffset.UtcNow;
        DbContext.QrScans.Add(new QrScan
        {
            Id = Guid.NewGuid(),
            TenantId = qr.TenantId,
            QrCodeId = qr.Id,
            CompetitionId = qr.CompetitionId,
            CampaignSourceId = qr.CampaignSourceId,
            OccurredAt = DateTimeOffset.UtcNow,
            IpHash = ipHash,
            UserAgentHash = HashNullable(userAgent),
            Referrer = referrer,
            IsUnique = unique
        });
        await DbContext.SaveChangesAsync(cancellationToken);
        return qr.DestinationUrl;
    }

    public async Task<AnalyticsOverviewResponse> OverviewAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        var scans = await DbContext.QrScans.LongCountAsync(x => x.CompetitionId == competitionId, cancellationToken);
        var uniqueScans = await DbContext.QrScans.LongCountAsync(x => x.CompetitionId == competitionId && x.IsUnique, cancellationToken);
        var submitted = await DbContext.CompetitionEntries.LongCountAsync(x => x.CompetitionId == competitionId, cancellationToken);
        var approved = await DbContext.CompetitionEntries.LongCountAsync(x => x.CompetitionId == competitionId && x.Status == CompetitionEntryStatus.Approved, cancellationToken);
        var rejected = await DbContext.CompetitionEntries.LongCountAsync(x => x.CompetitionId == competitionId && x.Status == CompetitionEntryStatus.Rejected, cancellationToken);
        var highRisk = await DbContext.CompetitionEntries.LongCountAsync(x => x.CompetitionId == competitionId && (x.RiskLevel == RiskLevel.High || x.RiskLevel == RiskLevel.Blocked), cancellationToken);
        var conversion = uniqueScans == 0 ? 0 : decimal.Round((decimal)submitted / uniqueScans * 100, 2);
        return new AnalyticsOverviewResponse(scans, uniqueScans, submitted, approved, rejected, highRisk, conversion);
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant().Replace(' ', '-');
    private static string ShortCode() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(8)).Replace("+", string.Empty).Replace("/", string.Empty).Replace("=", string.Empty)[..8].ToUpperInvariant();
    private static string? HashNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static CampaignSourceResponse ToResponse(CampaignSource source) => new(source.Id, source.CompetitionId, source.Name, source.SourceType.ToString(), source.Code, source.IsActive);
    private static QrCodeResponse ToResponse(QrCode qr) => new(qr.Id, qr.CompetitionId, qr.CampaignSourceId, qr.ShortCode, qr.DestinationUrl, qr.Status.ToString(), qr.ScanCount, qr.UniqueScanCount);
}
