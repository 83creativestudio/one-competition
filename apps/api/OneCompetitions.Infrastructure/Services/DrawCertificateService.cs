using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneCompetitions.Application.Billing;
using OneCompetitions.Application.Draws;
using OneCompetitions.Application.Storage;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Contracts.Draws;
using OneCompetitions.Domain.Draws;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using QRCoder;

namespace OneCompetitions.Infrastructure.Services;

public sealed class DrawCertificateService(
    AppDbContext dbContext,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    IObjectStorage storage,
    IPlanLimitService planLimits,
    IConfiguration configuration,
    ILogger<DrawCertificateService> logger)
    : TenantScopedServiceBase(dbContext, tenantContext, userManager), IDrawCertificateService
{
    private static readonly object FontLock = new();
    public async Task<DrawVerificationResponse?> GetPublicVerificationAsync(string drawReference, CancellationToken cancellationToken)
    {
        var data = await (from draw in DbContext.Draws.IgnoreQueryFilters()
                          join competition in DbContext.Competitions.IgnoreQueryFilters() on draw.CompetitionId equals competition.Id
                          join tenant in DbContext.Tenants.IgnoreQueryFilters() on draw.TenantId equals tenant.Id
                          join certificate in DbContext.DrawCertificates.IgnoreQueryFilters() on draw.Id equals certificate.DrawId into certificates
                          from certificate in certificates.DefaultIfEmpty()
                          where draw.DrawReference == drawReference
                          select new { draw, competition.Name, Organiser = tenant.Name, certificate }).SingleOrDefaultAsync(cancellationToken);
        if (data is null) return null;
        var valid = false;
        if (data.certificate is { Status: "Generated", StorageKey: not null, Sha256Hash: not null })
        {
            try
            {
                await using var stream = await storage.OpenReadAsync(data.certificate.StorageKey, cancellationToken);
                valid = string.Equals(Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)), data.certificate.Sha256Hash, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                valid = false;
            }
        }
        return new DrawVerificationResponse(data.draw.DrawReference, data.draw.Status.ToString(), data.Name, data.Organiser,
            data.draw.ExecutedAt, data.draw.EligibleEntryCount, data.draw.ExcludedEntryCount, data.draw.EntryPoolHash,
            data.draw.ConfigurationHash, data.draw.AlgorithmVersion, valid, data.certificate?.Sha256Hash);
    }

    public async Task<string> GetCertificateDownloadUrlAsync(Guid userId, Guid competitionId, Guid drawId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        await planLimits.EnsureAllowedAsync(TenantContext.TenantId, "AllowDrawCertificate", 1, cancellationToken);
        var certificate = await DbContext.DrawCertificates.SingleOrDefaultAsync(x => x.DrawId == drawId && x.Draw.CompetitionId == competitionId, cancellationToken)
            ?? throw new InvalidOperationException("Draw certificate is not ready.");
        if (certificate.Status != "Generated" || certificate.StorageKey is null) throw new InvalidOperationException("Draw certificate is not ready.");
        return await storage.CreateDownloadUrlAsync(certificate.StorageKey, TimeSpan.FromMinutes(10), cancellationToken);
    }

    public async Task<int> GeneratePendingAsync(CancellationToken cancellationToken)
    {
        var draws = await DbContext.Draws.IgnoreQueryFilters()
            .Where(x => x.Status == DrawStatus.Completed && !DbContext.DrawCertificates.IgnoreQueryFilters().Any(c => c.DrawId == x.Id))
            .Take(10).ToListAsync(cancellationToken);
        var generated = 0;
        foreach (var draw in draws)
        {
            try
            {
                await planLimits.EnsureAllowedAsync(draw.TenantId, "AllowDrawCertificate", 1, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                continue;
            }
            var certificate = new DrawCertificate { Id = Guid.NewGuid(), TenantId = draw.TenantId, DrawId = draw.Id, Status = "Generating", CreatedAt = DateTimeOffset.UtcNow };
            DbContext.DrawCertificates.Add(certificate);
            await DbContext.SaveChangesAsync(cancellationToken);
            try
            {
                var bytes = await BuildPdfAsync(draw, cancellationToken);
                certificate.StorageKey = $"tenant/{draw.TenantId:N}/draws/{draw.DrawReference}/certificate.pdf";
                await using var stream = new MemoryStream(bytes);
                await storage.PutAsync(certificate.StorageKey, stream, "application/pdf", cancellationToken);
                certificate.Sha256Hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
                certificate.Status = "Generated";
                certificate.GeneratedAt = DateTimeOffset.UtcNow;
                generated++;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Draw certificate generation failed for draw {DrawId}", draw.Id);
                certificate.Status = "Failed";
                certificate.FailureReason = exception.Message[..Math.Min(exception.Message.Length, 1000)];
            }
            await DbContext.SaveChangesAsync(cancellationToken);
        }
        return generated;
    }

    private async Task<byte[]> BuildPdfAsync(Draw draw, CancellationToken cancellationToken)
    {
        EnsureFontResolver();
        var competition = await DbContext.Competitions.IgnoreQueryFilters().SingleAsync(x => x.Id == draw.CompetitionId, cancellationToken);
        var tenant = await DbContext.Tenants.IgnoreQueryFilters().SingleAsync(x => x.Id == draw.TenantId, cancellationToken);
        var results = await (from result in DbContext.DrawResults
                             join entry in DbContext.CompetitionEntries.IgnoreQueryFilters() on result.EntryId equals entry.Id
                             where result.DrawId == draw.Id
                             orderby result.ResultType, result.Position
                             select new { Type = result.ResultType.ToString(), result.Position, entry.EntryReference }).ToListAsync(cancellationToken);
        var verifyBase = configuration["PLATFORM_VERIFY_DOMAIN"] ?? "verify.competitions.local";
        var verifyUrl = $"https://{verifyBase}/draw/{draw.DrawReference}";
        using var qrData = QRCodeGenerator.GenerateQrCode(verifyUrl, QRCodeGenerator.ECCLevel.Q);

        using var document = new PdfDocument();
        document.Info.Title = $"Draw certificate {draw.DrawReference}";
        var page = document.AddPage();
        page.Size = PdfSharp.PageSize.A4;
        using var graphics = XGraphics.FromPdfPage(page);
        var title = new XFont("ONE", 19, XFontStyleEx.Bold);
        var heading = new XFont("ONE", 11, XFontStyleEx.Bold);
        var body = new XFont("ONE", 10, XFontStyleEx.Regular);
        var y = 48d;
        graphics.DrawString("ONE. Competitions", title, XBrushes.Black, 48, y);
        y += 30;
        graphics.DrawString("AUDITABLE RANDOM DRAW CERTIFICATE", heading, XBrushes.DarkSlateGray, 48, y);
        y += 28;
        foreach (var line in new[]
        {
            ("Organiser", tenant.Name), ("Competition", competition.Name), ("Draw reference", draw.DrawReference),
            ("Opening date", competition.StartsAt.ToString("u")), ("Closing date", competition.EndsAt.ToString("u")),
            ("Draw date", draw.ExecutedAt?.ToString("u") ?? "Pending"), ("Eligible entries", draw.EligibleEntryCount.ToString()),
            ("Excluded entries", draw.ExcludedEntryCount.ToString()), ("Algorithm", draw.AlgorithmVersion),
            ("Entry pool hash", draw.EntryPoolHash), ("Configuration hash", draw.ConfigurationHash)
        })
        {
            graphics.DrawString(line.Item1, heading, XBrushes.Black, 48, y);
            graphics.DrawString(line.Item2, body, XBrushes.Black, 165, y);
            y += line.Item1.Contains("hash", StringComparison.OrdinalIgnoreCase) ? 30 : 19;
        }
        y += 8;
        graphics.DrawString("Selected entry references", heading, XBrushes.Black, 48, y);
        y += 20;
        foreach (var result in results)
        {
            graphics.DrawString($"{result.Type} {result.Position}: {result.EntryReference}", body, XBrushes.Black, 62, y);
            y += 17;
        }
        DrawQrCode(graphics, qrData, 430, 650, 105);
        graphics.DrawString("Verify certificate", heading, XBrushes.Black, 430, 775);
        graphics.DrawString(DateTimeOffset.UtcNow.ToString("u"), body, XBrushes.Gray, 48, 805);
        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    private static void EnsureFontResolver()
    {
        if (GlobalFontSettings.FontResolver is not null) return;
        lock (FontLock)
        {
            GlobalFontSettings.FontResolver ??= new CrossPlatformFontResolver();
        }
    }

    private static void DrawQrCode(XGraphics graphics, QRCodeData data, double x, double y, double size)
    {
        var count = data.ModuleMatrix.Count;
        var module = size / count;
        graphics.DrawRectangle(XBrushes.White, x, y, size, size);
        for (var row = 0; row < count; row++)
        {
            for (var column = 0; column < count; column++)
            {
                if (data.ModuleMatrix[row][column])
                    graphics.DrawRectangle(XBrushes.Black, x + column * module, y + row * module, module + 0.1, module + 0.1);
            }
        }
    }
}
