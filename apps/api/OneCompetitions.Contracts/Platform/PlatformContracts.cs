using OneCompetitions.Contracts.Billing;
using OneCompetitions.Contracts.Domains;
using OneCompetitions.Contracts.Tenants;

namespace OneCompetitions.Contracts.Platform;

public sealed record PlatformTenantDetailResponse(TenantResponse Tenant, int ActiveUsers, IReadOnlyList<TenantDomainResponse> Domains, SubscriptionResponse? Subscription);
public sealed record UpdateTenantStatusRequest(string Status, string Reason);
public sealed record PlatformDomainResponse(Guid Id, Guid TenantId, string TenantName, string Hostname, string DomainType, string Status, string? SslStatus, DateTimeOffset? CertificateExpiresAt);
public sealed record FeatureFlagResponse(Guid Id, string Code, string Name, bool IsEnabled, string Environment);
public sealed record UpsertFeatureFlagRequest(string Code, string Name, bool IsEnabled, string Environment);
public sealed record UpsertPlanRequest(string Name, string Code, decimal MonthlyPrice, decimal AnnualPrice, string FeatureConfigurationJson, bool IsActive);
public sealed record ResellerResponse(Guid Id, string Name, string Slug, string Status, int TenantCount);
public sealed record UpsertResellerRequest(string Name, string Slug, string Status);
public sealed record AuditEventResponse(Guid Id, Guid TenantId, Guid? ActorUserId, string ActorType, string Action, string EntityType, string? EntityId, DateTimeOffset OccurredAt, string CorrelationId);
