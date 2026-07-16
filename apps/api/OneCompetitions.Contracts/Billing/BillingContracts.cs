namespace OneCompetitions.Contracts.Billing;

public sealed record PlanResponse(Guid Id, string Name, string Code, decimal MonthlyPrice, decimal AnnualPrice, IReadOnlyDictionary<string, object?> Features);
public sealed record SubscriptionResponse(Guid Id, Guid PlanId, string Status, DateTimeOffset CurrentPeriodStartsAt, DateTimeOffset CurrentPeriodEndsAt, DateTimeOffset? TrialEndsAt);
public sealed record BillingOverviewResponse(IReadOnlyList<PlanResponse> Plans, SubscriptionResponse? Subscription);
public sealed record CreateCheckoutSessionRequest(Guid PlanId, string BillingPeriod, string SuccessUrl, string CancelUrl);
public sealed record CheckoutSessionResponse(string CheckoutUrl);
