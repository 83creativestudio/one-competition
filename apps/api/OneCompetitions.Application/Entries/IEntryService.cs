using OneCompetitions.Contracts.Entries;

namespace OneCompetitions.Application.Entries;

public interface IEntryService
{
    Task<EntryResponse> SubmitAsync(string competitionSlug, SubmitEntryRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken);
    Task<IReadOnlyList<EntryResponse>> ListAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken);
    Task<EntryResponse?> GetAsync(Guid userId, Guid competitionId, Guid entryId, CancellationToken cancellationToken);
    Task<EntryResponse> ReviewAsync(Guid userId, Guid competitionId, Guid entryId, EntryReviewRequest request, CancellationToken cancellationToken);
    Task<EntryResponse?> GetPublicStatusAsync(string reference, CancellationToken cancellationToken);
}
