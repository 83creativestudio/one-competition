namespace OneCompetitions.Application.Entries;

/// <summary>Validates a public-entry anti-bot challenge without coupling the application to a provider.</summary>
public interface ICaptchaProvider
{
    Task<bool> ValidateAsync(string? token, string? remoteIpAddress, CancellationToken cancellationToken);
}
