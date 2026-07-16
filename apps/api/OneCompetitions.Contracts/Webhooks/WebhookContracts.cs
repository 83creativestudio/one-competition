namespace OneCompetitions.Contracts.Webhooks;

public sealed record CreateWebhookEndpointRequest(string Url, IReadOnlyList<string> EventTypes);
public sealed record WebhookEndpointResponse(Guid Id, string Url, IReadOnlyList<string> EventTypes, bool IsActive, int ConsecutiveFailureCount, DateTimeOffset CreatedAt);
public sealed record WebhookEndpointCreatedResponse(WebhookEndpointResponse Endpoint, string SigningSecret);
