using OneCompetitions.Contracts.Draws;

namespace OneCompetitions.Application.Draws;

public interface IDrawService
{
    Task<IReadOnlyList<DrawResponse>> ListAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken);
    Task<DrawResponse> PrepareAsync(Guid userId, Guid competitionId, PrepareDrawRequest request, CancellationToken cancellationToken);
    Task<DrawResponse> ApproveAsync(Guid userId, Guid competitionId, Guid drawId, CancellationToken cancellationToken);
    Task<DrawResponse> ExecuteAsync(Guid userId, Guid competitionId, Guid drawId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DrawResultResponse>> ResultsAsync(Guid userId, Guid competitionId, Guid drawId, CancellationToken cancellationToken);
}
