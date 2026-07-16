using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OneCompetitions.Application.Billing;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Contracts.Billing;
using OneCompetitions.Domain.Billing;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;
using Stripe;
using Stripe.Checkout;
using BillingPlan = OneCompetitions.Domain.Billing.Plan;

namespace OneCompetitions.Infrastructure.Services;

public sealed class BillingService(
    AppDbContext dbContext,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    IHostEnvironment environment)
    : TenantScopedServiceBase(dbContext, tenantContext, userManager), IBillingService
{
    public async Task<BillingOverviewResponse> GetOverviewAsync(Guid userId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        var plans = await DbContext.Plans.Where(x => x.IsActive).OrderBy(x => x.MonthlyPrice).ToListAsync(cancellationToken);
        var subscription = (await DbContext.TenantSubscriptions.ToListAsync(cancellationToken)).MaxBy(x => x.CreatedAt);
        return new BillingOverviewResponse(plans.Select(ToResponse).ToList(), subscription is null ? null : ToResponse(subscription));
    }

    public async Task<CheckoutSessionResponse> CreateCheckoutAsync(Guid userId, CreateCheckoutSessionRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        ValidateRedirect(request.SuccessUrl);
        ValidateRedirect(request.CancelUrl);
        var plan = await DbContext.Plans.SingleOrDefaultAsync(x => x.Id == request.PlanId && x.IsActive, cancellationToken) ?? throw new InvalidOperationException("Plan was not found.");
        var priceId = configuration[$"STRIPE_PRICE_{plan.Code.ToUpperInvariant()}_{request.BillingPeriod.ToUpperInvariant()}"];
        if (string.IsNullOrWhiteSpace(priceId)) throw new InvalidOperationException("Stripe price is not configured for this plan and billing period.");
        StripeConfiguration.ApiKey = configuration["STRIPE_SECRET_KEY"] ?? throw new InvalidOperationException("STRIPE_SECRET_KEY is required.");
        var session = await new SessionService().CreateAsync(new SessionCreateOptions
        {
            Mode = "subscription", SuccessUrl = request.SuccessUrl, CancelUrl = request.CancelUrl,
            LineItems = [new SessionLineItemOptions { Price = priceId, Quantity = 1 }],
            Metadata = new Dictionary<string, string> { ["tenant_id"] = TenantContext.TenantId.ToString(), ["plan_id"] = plan.Id.ToString() },
            SubscriptionData = new SessionSubscriptionDataOptions { Metadata = new Dictionary<string, string> { ["tenant_id"] = TenantContext.TenantId.ToString(), ["plan_id"] = plan.Id.ToString() } }
        }, cancellationToken: cancellationToken);
        return new CheckoutSessionResponse(session.Url);
    }

    public async Task HandleWebhookAsync(string payload, string signature, CancellationToken cancellationToken)
    {
        var secret = configuration["STRIPE_WEBHOOK_SECRET"] ?? throw new InvalidOperationException("STRIPE_WEBHOOK_SECRET is required.");
        var stripeEvent = EventUtility.ConstructEvent(payload, signature, secret);
        if (stripeEvent.Data.Object is Stripe.Subscription subscription && subscription.Metadata.TryGetValue("tenant_id", out var tenantText)
            && subscription.Metadata.TryGetValue("plan_id", out var planText) && Guid.TryParse(tenantText, out var tenantId) && Guid.TryParse(planText, out var planId))
        {
            if (await DbContext.BillingWebhookEvents.IgnoreQueryFilters().AnyAsync(x => x.Provider == "Stripe" && x.ExternalEventId == stripeEvent.Id, cancellationToken)) return;
            var entity = await DbContext.TenantSubscriptions.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.ExternalSubscriptionId == subscription.Id, cancellationToken);
            entity ??= new TenantSubscription { Id = Guid.NewGuid(), TenantId = tenantId, CreatedAt = DateTimeOffset.UtcNow, StartedAt = DateTimeOffset.UtcNow };
            if (DbContext.Entry(entity).State == EntityState.Detached) DbContext.TenantSubscriptions.Add(entity);
            entity.PlanId = planId;
            entity.Status = subscription.Status;
            entity.ExternalProvider = "Stripe";
            entity.ExternalSubscriptionId = subscription.Id;
            entity.ExternalCustomerId = subscription.CustomerId;
            entity.CancelAtPeriodEnd = subscription.CancelAtPeriodEnd;
            entity.CurrentPeriodStartsAt = subscription.Items.Data.Min(x => x.CurrentPeriodStart);
            entity.CurrentPeriodEndsAt = subscription.Items.Data.Max(x => x.CurrentPeriodEnd);
            entity.CancelledAt = subscription.Status == "canceled" ? DateTimeOffset.UtcNow : null;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            var tenant = await DbContext.Tenants.IgnoreQueryFilters().SingleAsync(x => x.Id == tenantId, cancellationToken);
            tenant.PlanId = planId;
            tenant.Status = subscription.Status switch
            {
                "active" => Domain.Tenants.TenantStatus.Active,
                "trialing" => Domain.Tenants.TenantStatus.Trial,
                "past_due" or "unpaid" => Domain.Tenants.TenantStatus.PastDue,
                "canceled" => Domain.Tenants.TenantStatus.Cancelled,
                _ => tenant.Status
            };
            tenant.SuspendedAt = tenant.Status == Domain.Tenants.TenantStatus.Suspended ? DateTimeOffset.UtcNow : null;
            tenant.UpdatedAt = DateTimeOffset.UtcNow;
            DbContext.BillingWebhookEvents.Add(new BillingWebhookEvent
            {
                Id = Guid.NewGuid(), TenantId = tenantId, Provider = "Stripe", ExternalEventId = stripeEvent.Id,
                EventType = stripeEvent.Type, ProcessedAt = DateTimeOffset.UtcNow
            });
            await DbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<BillingPortalResponse> CreatePortalAsync(Guid userId, CreateBillingPortalRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        ValidateRedirect(request.ReturnUrl);
        var subscription = (await DbContext.TenantSubscriptions.Where(x => x.ExternalProvider == "Stripe").ToListAsync(cancellationToken)).MaxBy(x => x.CreatedAt)
            ?? throw new InvalidOperationException("An active Stripe subscription was not found.");
        if (string.IsNullOrWhiteSpace(subscription.ExternalCustomerId)) throw new InvalidOperationException("Stripe customer information is unavailable.");
        StripeConfiguration.ApiKey = configuration["STRIPE_SECRET_KEY"] ?? throw new InvalidOperationException("STRIPE_SECRET_KEY is required.");
        var session = await new Stripe.BillingPortal.SessionService().CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = subscription.ExternalCustomerId,
            ReturnUrl = request.ReturnUrl
        }, cancellationToken: cancellationToken);
        return new BillingPortalResponse(session.Url);
    }

    public async Task CancelAsync(Guid userId, CancelSubscriptionRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var subscription = (await DbContext.TenantSubscriptions.Where(x => x.ExternalProvider == "Stripe").ToListAsync(cancellationToken)).MaxBy(x => x.CreatedAt)
            ?? throw new InvalidOperationException("An active Stripe subscription was not found.");
        if (string.IsNullOrWhiteSpace(subscription.ExternalSubscriptionId)) throw new InvalidOperationException("Stripe subscription information is unavailable.");
        StripeConfiguration.ApiKey = configuration["STRIPE_SECRET_KEY"] ?? throw new InvalidOperationException("STRIPE_SECRET_KEY is required.");
        var service = new Stripe.SubscriptionService();
        if (request.AtPeriodEnd)
        {
            await service.UpdateAsync(subscription.ExternalSubscriptionId, new SubscriptionUpdateOptions { CancelAtPeriodEnd = true }, cancellationToken: cancellationToken);
            subscription.CancelAtPeriodEnd = true;
        }
        else
        {
            await service.CancelAsync(subscription.ExternalSubscriptionId, cancellationToken: cancellationToken);
            subscription.Status = "canceled";
            subscription.CancelledAt = DateTimeOffset.UtcNow;
        }
        subscription.UpdatedAt = DateTimeOffset.UtcNow;
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    private void ValidateRedirect(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            if (!(environment.IsDevelopment() && Uri.TryCreate(value, UriKind.Absolute, out uri) && uri.Host is "localhost" or "127.0.0.1"))
                throw new InvalidOperationException("Checkout redirect URL must be an allowed HTTPS URL.");
        }
        var baseDomain = configuration["PLATFORM_BASE_DOMAIN"];
        if (!environment.IsDevelopment() && !string.IsNullOrWhiteSpace(baseDomain) && uri is not null && !(uri.Host.Equals(baseDomain) || uri.Host.EndsWith($".{baseDomain}")))
            throw new InvalidOperationException("Checkout redirect URL is outside the platform domain.");
    }

    private static PlanResponse ToResponse(BillingPlan plan) => new(plan.Id, plan.Name, plan.Code, plan.MonthlyPrice, plan.AnnualPrice,
        JsonSerializer.Deserialize<Dictionary<string, object?>>(plan.FeatureConfigurationJson) ?? []);
    private static SubscriptionResponse ToResponse(TenantSubscription value) => new(value.Id, value.PlanId, value.Status, value.CurrentPeriodStartsAt, value.CurrentPeriodEndsAt, value.TrialEndsAt, value.CancelAtPeriodEnd);
}

public sealed class PlanLimitService(AppDbContext dbContext) : IPlanLimitService
{
    public async Task EnsureAllowedAsync(Guid tenantId, string feature, int increment, CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants.IgnoreQueryFilters().SingleAsync(x => x.Id == tenantId, cancellationToken);
        if (tenant.Status is Domain.Tenants.TenantStatus.Suspended or Domain.Tenants.TenantStatus.Cancelled or Domain.Tenants.TenantStatus.Archived)
            throw new InvalidOperationException("Tenant subscription is not permitted to create or operate resources.");
        var subscription = (await dbContext.TenantSubscriptions.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            .ToListAsync(cancellationToken)).MaxBy(x => x.CreatedAt);
        if (subscription is not null && (!new[] { "active", "trialing", "trial" }.Contains(subscription.Status, StringComparer.OrdinalIgnoreCase)
            || subscription.CurrentPeriodEndsAt <= DateTimeOffset.UtcNow))
            throw new InvalidOperationException("Tenant subscription is not active.");
        var plan = tenant.PlanId is not null
            ? await dbContext.Plans.SingleOrDefaultAsync(x => x.Id == tenant.PlanId, cancellationToken)
            : await dbContext.Plans.OrderBy(x => x.MonthlyPrice).FirstOrDefaultAsync(cancellationToken);
        if (plan is null) throw new InvalidOperationException("Tenant does not have an active plan.");
        using var config = JsonDocument.Parse(plan.FeatureConfigurationJson);
        if (!config.RootElement.TryGetProperty(feature, out var limit)) throw new InvalidOperationException($"Feature '{feature}' is not included in the current plan.");
        if (limit.ValueKind is JsonValueKind.False) throw new InvalidOperationException($"Feature '{feature}' is not included in the current plan.");
        if (limit.ValueKind is JsonValueKind.Number)
        {
            var maximum = limit.GetInt32();
            var current = feature switch
            {
                "MaximumActiveCompetitions" => await dbContext.Competitions.IgnoreQueryFilters().CountAsync(x => x.TenantId == tenantId && x.DeletedAt == null && x.Status != Domain.Competitions.CompetitionStatus.Archived && x.Status != Domain.Competitions.CompetitionStatus.Cancelled, cancellationToken),
                "MaximumCustomDomains" => await dbContext.TenantDomains.IgnoreQueryFilters().CountAsync(x => x.TenantId == tenantId && x.DeletedAt == null && x.DomainType != Domain.Tenants.TenantDomainType.PlatformPath && x.DomainType != Domain.Tenants.TenantDomainType.PlatformSubdomain, cancellationToken),
                _ => 0
            };
            if (current + increment > maximum) throw new InvalidOperationException($"Plan limit '{feature}' of {maximum} has been reached.");
        }
    }
}
