namespace OneCompetitions.Contracts.Privacy;

public sealed record CreatePrivacyRequest(string Email, string RequestType);
public sealed record PrivacyRequestAcceptedResponse(string Message);
public sealed record CompletePrivacyRequest(string Token);
public sealed record PrivacyRequestResult(string Reference, string RequestType, string Status, string? DownloadUrl, DateTimeOffset? CompletedAt);
