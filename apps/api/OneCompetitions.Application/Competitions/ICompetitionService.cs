using OneCompetitions.Contracts.Competitions;
using OneCompetitions.Contracts.Entries;

namespace OneCompetitions.Application.Competitions;

public interface ICompetitionService
{
    Task<IReadOnlyList<CompetitionResponse>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<CompetitionResponse> CreateAsync(Guid userId, CreateCompetitionRequest request, CancellationToken cancellationToken);
    Task<CompetitionResponse?> GetAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken);
    Task<CompetitionResponse> UpdateAsync(Guid userId, Guid competitionId, UpdateCompetitionRequest request, CancellationToken cancellationToken);
    Task<CompetitionResponse> PublishAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken);
    Task<CompetitionResponse> CloseAsync(Guid userId, Guid competitionId, string? reason, CancellationToken cancellationToken);
    Task<IReadOnlyList<CompetitionVersionResponse>> VersionsAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CompetitionFieldResponse>> ListFieldsAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken);
    Task<CompetitionFieldResponse> AddFieldAsync(Guid userId, Guid competitionId, UpsertCompetitionFieldRequest request, CancellationToken cancellationToken);
    Task<CompetitionFieldResponse> UpdateFieldAsync(Guid userId, Guid competitionId, Guid fieldId, UpsertCompetitionFieldRequest request, CancellationToken cancellationToken);
    Task DeleteFieldAsync(Guid userId, Guid competitionId, Guid fieldId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CompetitionFieldResponse>> ReorderFieldsAsync(Guid userId, Guid competitionId, ReorderCompetitionFieldsRequest request, CancellationToken cancellationToken);
    Task<RulesVersionResponse> AddRulesAsync(Guid userId, Guid competitionId, CreateRulesVersionRequest request, CancellationToken cancellationToken);
    Task<CompetitionPageResponse> UpsertPageAsync(Guid userId, Guid competitionId, UpsertCompetitionPageRequest request, CancellationToken cancellationToken);
    Task<PublicCompetitionResponse?> GetPublicAsync(string slug, CancellationToken cancellationToken);
}
