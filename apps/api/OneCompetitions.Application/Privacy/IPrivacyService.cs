using OneCompetitions.Contracts.Privacy;

namespace OneCompetitions.Application.Privacy;

public interface IPrivacyService
{
    Task RequestAsync(CreatePrivacyRequest request, CancellationToken cancellationToken);
    Task<PrivacyRequestResult> CompleteAsync(string reference, CompletePrivacyRequest request, CancellationToken cancellationToken);
}
