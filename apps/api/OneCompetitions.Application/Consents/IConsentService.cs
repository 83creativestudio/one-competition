using OneCompetitions.Contracts.Consents;

namespace OneCompetitions.Application.Consents;

public interface IConsentService
{
    Task<IReadOnlyList<ConsentDefinitionResponse>> ListAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken);
    Task<ConsentDefinitionResponse> CreateVersionAsync(Guid userId, Guid competitionId, CreateConsentDefinitionRequest request, CancellationToken cancellationToken);
}
