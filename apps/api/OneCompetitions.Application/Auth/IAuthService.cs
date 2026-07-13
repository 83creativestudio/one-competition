using OneCompetitions.Contracts.Auth;

namespace OneCompetitions.Application.Auth;

/// <summary>
/// Handles secure dashboard authentication and refresh-token sessions.
/// </summary>
public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken);
    Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken);
    Task<IReadOnlyList<SessionResponse>> GetSessionsAsync(Guid userId, CancellationToken cancellationToken);
    Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
}
