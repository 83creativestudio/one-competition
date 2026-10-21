using OneCompetitions.Contracts.Auth;

namespace OneCompetitions.Application.Auth;

/// <summary>Registers isolated customer workspaces and manages accounts through verified platform administrators.</summary>
public interface IAccountManagementService
{
    Task RegisterAsync(RegisterCustomerRequest request, CancellationToken cancellationToken);
    Task RequestVerificationAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);
    Task<AccountPageResponse> ListAsync(Guid actorId, string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<AccountResponse> CreateAsync(Guid actorId, CreateAccountRequest request, CancellationToken cancellationToken);
    Task<AccountResponse> UpdateAsync(Guid actorId, Guid userId, UpdateAccountRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid actorId, Guid userId, CancellationToken cancellationToken);
}
