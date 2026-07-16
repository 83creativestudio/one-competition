using OneCompetitions.Contracts.Exports;

namespace OneCompetitions.Application.Exports;

public interface IExportService
{
    Task<ExportJobResponse> CreateAsync(Guid userId, Guid competitionId, CreateExportRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExportJobResponse>> ListAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken);
    Task<string> GetDownloadUrlAsync(Guid userId, Guid competitionId, Guid exportId, CancellationToken cancellationToken);
}
