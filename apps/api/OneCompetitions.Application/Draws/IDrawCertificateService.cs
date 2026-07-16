using OneCompetitions.Contracts.Draws;

namespace OneCompetitions.Application.Draws;

public interface IDrawCertificateService
{
    Task<DrawVerificationResponse?> GetPublicVerificationAsync(string drawReference, CancellationToken cancellationToken);
    Task<string> GetCertificateDownloadUrlAsync(Guid userId, Guid competitionId, Guid drawId, CancellationToken cancellationToken);
    Task<int> GeneratePendingAsync(CancellationToken cancellationToken);
}
