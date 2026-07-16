using OneCompetitions.Contracts.Webhooks;

namespace OneCompetitions.Application.Webhooks;

public interface IWebhookService
{
    Task<IReadOnlyList<WebhookEndpointResponse>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<WebhookEndpointCreatedResponse> CreateAsync(Guid userId, CreateWebhookEndpointRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid endpointId, CancellationToken cancellationToken);
    Task QueueEventAsync(Guid tenantId, string eventType, object payload, CancellationToken cancellationToken);
}
