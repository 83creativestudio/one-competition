using OneCompetitions.Contracts.Winners;

namespace OneCompetitions.Application.Winners;

public interface IWinnerService
{
    Task<IReadOnlyList<WinnerClaimResponse>> ListAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken);
    Task<WinnerClaimResponse> ContactAsync(Guid userId, Guid competitionId, Guid winnerId, WinnerActionRequest request, CancellationToken cancellationToken);
    Task<WinnerClaimResponse> AcceptAsync(Guid userId, Guid competitionId, Guid winnerId, CancellationToken cancellationToken);
    Task<WinnerClaimResponse> DisqualifyAsync(Guid userId, Guid competitionId, Guid winnerId, WinnerActionRequest request, CancellationToken cancellationToken);
    Task<WinnerClaimResponse> DeliverPrizeAsync(Guid userId, Guid competitionId, Guid winnerId, CancellationToken cancellationToken);
}
