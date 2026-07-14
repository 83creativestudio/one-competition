using OneCompetitions.Contracts.Analytics;
using OneCompetitions.Contracts.Campaigns;
using OneCompetitions.Contracts.Qr;

namespace OneCompetitions.Application.Campaigns;

public interface ICampaignService
{
    Task<IReadOnlyList<CampaignSourceResponse>> ListSourcesAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken);
    Task<CampaignSourceResponse> CreateSourceAsync(Guid userId, Guid competitionId, CreateCampaignSourceRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<QrCodeResponse>> ListQrCodesAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken);
    Task<QrCodeResponse> CreateQrCodeAsync(Guid userId, Guid competitionId, CreateQrCodeRequest request, CancellationToken cancellationToken);
    Task<string> RecordQrScanAndGetRedirectAsync(string shortCode, string? ipAddress, string? userAgent, string? referrer, CancellationToken cancellationToken);
    Task<AnalyticsOverviewResponse> OverviewAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken);
}
