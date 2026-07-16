using OneCompetitions.Contracts.Billing;

namespace OneCompetitions.Application.Billing;

public interface IBillingService
{
    Task<BillingOverviewResponse> GetOverviewAsync(Guid userId, CancellationToken cancellationToken);
    Task<CheckoutSessionResponse> CreateCheckoutAsync(Guid userId, CreateCheckoutSessionRequest request, CancellationToken cancellationToken);
    Task HandleWebhookAsync(string payload, string signature, CancellationToken cancellationToken);
}

public interface IPlanLimitService
{
    Task EnsureAllowedAsync(Guid tenantId, string feature, int increment, CancellationToken cancellationToken);
}
