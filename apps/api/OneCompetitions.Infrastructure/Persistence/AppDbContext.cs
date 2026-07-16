using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Domain.Analytics;
using OneCompetitions.Domain.Assets;
using OneCompetitions.Domain.Auditing;
using OneCompetitions.Domain.Billing;
using OneCompetitions.Domain.Branding;
using OneCompetitions.Domain.Campaigns;
using OneCompetitions.Domain.Competitions;
using OneCompetitions.Domain.Consents;
using OneCompetitions.Domain.Draws;
using OneCompetitions.Domain.Entries;
using OneCompetitions.Domain.Exports;
using OneCompetitions.Domain.Features;
using OneCompetitions.Domain.Fraud;
using OneCompetitions.Domain.Notifications;
using OneCompetitions.Domain.Participants;
using OneCompetitions.Domain.Privacy;
using OneCompetitions.Domain.Qr;
using OneCompetitions.Domain.Tenants;
using OneCompetitions.Domain.Webhooks;
using OneCompetitions.Domain.Winners;
using OneCompetitions.Infrastructure.Identity;

namespace OneCompetitions.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ITenantContext tenantContext)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Reseller> Resellers => Set<Reseller>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<TenantDomain> TenantDomains => Set<TenantDomain>();
    public DbSet<BrandProfile> BrandProfiles => Set<BrandProfile>();
    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<CompetitionVersion> CompetitionVersions => Set<CompetitionVersion>();
    public DbSet<CompetitionPage> CompetitionPages => Set<CompetitionPage>();
    public DbSet<CompetitionField> CompetitionFields => Set<CompetitionField>();
    public DbSet<CompetitionRulesVersion> CompetitionRulesVersions => Set<CompetitionRulesVersion>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<ParticipantIdentity> ParticipantIdentities => Set<ParticipantIdentity>();
    public DbSet<CompetitionEntry> CompetitionEntries => Set<CompetitionEntry>();
    public DbSet<EntryAnswer> EntryAnswers => Set<EntryAnswer>();
    public DbSet<EntryVerification> EntryVerifications => Set<EntryVerification>();
    public DbSet<ConsentDefinition> ConsentDefinitions => Set<ConsentDefinition>();
    public DbSet<ParticipantConsent> ParticipantConsents => Set<ParticipantConsent>();
    public DbSet<CampaignSource> CampaignSources => Set<CampaignSource>();
    public DbSet<QrCode> QrCodes => Set<QrCode>();
    public DbSet<QrScan> QrScans => Set<QrScan>();
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();
    public DbSet<FraudRule> FraudRules => Set<FraudRule>();
    public DbSet<EntryRiskSignal> EntryRiskSignals => Set<EntryRiskSignal>();
    public DbSet<EntryReview> EntryReviews => Set<EntryReview>();
    public DbSet<Draw> Draws => Set<Draw>();
    public DbSet<DrawEntrySnapshot> DrawEntrySnapshots => Set<DrawEntrySnapshot>();
    public DbSet<DrawResult> DrawResults => Set<DrawResult>();
    public DbSet<DrawCertificate> DrawCertificates => Set<DrawCertificate>();
    public DbSet<WinnerClaim> WinnerClaims => Set<WinnerClaim>();
    public DbSet<WinnerContactAttempt> WinnerContactAttempts => Set<WinnerContactAttempt>();
    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<BillingWebhookEvent> BillingWebhookEvents => Set<BillingWebhookEvent>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<TenantFeatureOverride> TenantFeatureOverrides => Set<TenantFeatureOverride>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<DataRetentionPolicy> DataRetentionPolicies => Set<DataRetentionPolicy>();
    public DbSet<PrivacyRequest> PrivacyRequests => Set<PrivacyRequest>();
    public DbSet<WebhookEndpoint> WebhookEndpoints => Set<WebhookEndpoint>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<NotificationMessage> NotificationMessages => Set<NotificationMessage>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.LegalName).HasMaxLength(250);
            entity.Property(x => x.Slug).HasMaxLength(120);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.HasQueryFilter(x => x.DeletedAt == null);
        });

        builder.Entity<Reseller>(entity =>
        {
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Slug).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(40);
        });

        builder.Entity<TenantUser>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(80);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.HasOne(x => x.Tenant).WithMany(x => x.Users).HasForeignKey(x => x.TenantId);
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<TenantDomain>(entity =>
        {
            entity.HasIndex(x => x.Hostname).IsUnique();
            entity.Property(x => x.Hostname).HasMaxLength(255);
            entity.Property(x => x.DomainType).HasConversion<string>().HasMaxLength(60);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(60);
            entity.HasOne(x => x.Tenant).WithMany(x => x.Domains).HasForeignKey(x => x.TenantId);
            entity.HasQueryFilter(x => x.DeletedAt == null && (!tenantContext.IsResolved || x.TenantId == tenantContext.TenantId));
        });

        builder.Entity<BrandProfile>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.Property(x => x.PrimaryColor).HasMaxLength(20);
            entity.Property(x => x.SecondaryColor).HasMaxLength(20);
            entity.Property(x => x.AccentColor).HasMaxLength(20);
            entity.Property(x => x.BackgroundColor).HasMaxLength(20);
            entity.Property(x => x.TextColor).HasMaxLength(20);
            entity.Property(x => x.HeadingFont).HasMaxLength(120);
            entity.Property(x => x.BodyFont).HasMaxLength(120);
            entity.Property(x => x.ButtonStyle).HasMaxLength(40);
            entity.Property(x => x.FooterText).HasMaxLength(1000);
            entity.Property(x => x.SupportEmail).HasMaxLength(320);
            entity.Property(x => x.SupportPhone).HasMaxLength(80);
            entity.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId);
            entity.HasQueryFilter(x => x.DeletedAt == null && (!tenantContext.IsResolved || x.TenantId == tenantContext.TenantId));
        });

        builder.Entity<Competition>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Slug).HasMaxLength(140);
            entity.Property(x => x.InternalReference).HasMaxLength(120);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(60);
            entity.Property(x => x.CompetitionType).HasConversion<string>().HasMaxLength(80);
            entity.Property(x => x.DefaultLanguage).HasMaxLength(12);
            entity.Property(x => x.TimeZone).HasMaxLength(80);
            entity.Property(x => x.EligibilityMode).HasMaxLength(80);
            entity.HasQueryFilter(x => x.DeletedAt == null && (!tenantContext.IsResolved || x.TenantId == tenantContext.TenantId));
        });

        builder.Entity<CompetitionVersion>(entity =>
        {
            entity.HasIndex(x => new { x.CompetitionId, x.VersionNumber }).IsUnique();
            entity.HasOne(x => x.Competition).WithMany().HasForeignKey(x => x.CompetitionId);
        });

        builder.Entity<CompetitionPage>(entity =>
        {
            entity.HasIndex(x => new { x.CompetitionId, x.LanguageCode }).IsUnique();
            entity.Property(x => x.LanguageCode).HasMaxLength(12);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.Title).HasMaxLength(220);
            entity.Property(x => x.SeoTitle).HasMaxLength(220);
            entity.Property(x => x.SeoDescription).HasMaxLength(500);
            entity.HasOne(x => x.Competition).WithMany().HasForeignKey(x => x.CompetitionId);
        });

        builder.Entity<CompetitionField>(entity =>
        {
            entity.HasIndex(x => new { x.CompetitionId, x.FieldKey }).IsUnique();
            entity.Property(x => x.FieldKey).HasMaxLength(120);
            entity.Property(x => x.FieldType).HasConversion<string>().HasMaxLength(60);
            entity.Property(x => x.Label).HasMaxLength(220);
            entity.HasOne(x => x.Competition).WithMany().HasForeignKey(x => x.CompetitionId);
        });

        builder.Entity<CompetitionRulesVersion>(entity =>
        {
            entity.HasIndex(x => new { x.CompetitionId, x.VersionNumber, x.LanguageCode }).IsUnique();
            entity.Property(x => x.LanguageCode).HasMaxLength(12);
            entity.Property(x => x.Title).HasMaxLength(220);
            entity.Property(x => x.ContentHash).HasMaxLength(128);
            entity.HasOne(x => x.Competition).WithMany().HasForeignKey(x => x.CompetitionId);
        });

        builder.Entity<Participant>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PrimaryEmail });
            entity.HasIndex(x => new { x.TenantId, x.PrimaryPhone });
            entity.Property(x => x.PrimaryEmail).HasMaxLength(320);
            entity.Property(x => x.PrimaryPhone).HasMaxLength(80);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.HasQueryFilter(x => x.DeletedAt == null && (!tenantContext.IsResolved || x.TenantId == tenantContext.TenantId));
        });

        builder.Entity<ParticipantIdentity>(entity =>
        {
            entity.HasIndex(x => new { x.Provider, x.ProviderSubjectHash }).IsUnique();
            entity.Property(x => x.Provider).HasMaxLength(80);
            entity.Property(x => x.ProviderSubjectHash).HasMaxLength(128);
            entity.HasOne(x => x.Participant).WithMany().HasForeignKey(x => x.ParticipantId);
        });

        builder.Entity<CompetitionEntry>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.EntryReference }).IsUnique();
            entity.HasIndex(x => new { x.CompetitionId, x.IdempotencyKeyHash }).IsUnique();
            entity.HasIndex(x => new { x.CompetitionId, x.ParticipantId });
            entity.Property(x => x.EntryReference).HasMaxLength(80);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(60);
            entity.Property(x => x.EligibilityStatus).HasConversion<string>().HasMaxLength(60);
            entity.Property(x => x.RiskLevel).HasConversion<string>().HasMaxLength(40);
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<EntryAnswer>(entity =>
        {
            entity.HasIndex(x => new { x.EntryId, x.CompetitionFieldId }).IsUnique();
            entity.HasOne(x => x.Entry).WithMany().HasForeignKey(x => x.EntryId);
        });

        builder.Entity<EntryVerification>(entity =>
        {
            entity.HasIndex(x => new { x.EntryId, x.Channel }).IsUnique();
            entity.Property(x => x.Channel).HasMaxLength(20);
            entity.Property(x => x.TokenHash).HasMaxLength(128);
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<ConsentDefinition>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.CompetitionId, x.ConsentType, x.LanguageCode, x.Version }).IsUnique();
            entity.Property(x => x.ConsentType).HasConversion<string>().HasMaxLength(80);
            entity.Property(x => x.LanguageCode).HasMaxLength(12);
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<ParticipantConsent>(entity =>
        {
            entity.HasIndex(x => new { x.EntryId, x.ConsentDefinitionId }).IsUnique();
            entity.Property(x => x.ConsentTextHash).HasMaxLength(128);
        });

        builder.Entity<CampaignSource>(entity =>
        {
            entity.HasIndex(x => new { x.CompetitionId, x.Code }).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(180);
            entity.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(60);
            entity.Property(x => x.Code).HasMaxLength(80);
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<QrCode>(entity =>
        {
            entity.HasIndex(x => x.ShortCode).IsUnique();
            entity.Property(x => x.ShortCode).HasMaxLength(32);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<QrScan>(entity =>
        {
            entity.HasIndex(x => new { x.QrCodeId, x.OccurredAt });
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<AnalyticsEvent>(entity =>
        {
            entity.HasIndex(x => new { x.CompetitionId, x.OccurredAt });
            entity.Property(x => x.EventType).HasMaxLength(80);
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<FraudRule>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.RuleType });
            entity.Property(x => x.Name).HasMaxLength(180);
            entity.Property(x => x.RuleType).HasMaxLength(80);
            entity.Property(x => x.Action).HasMaxLength(80);
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<EntryRiskSignal>(entity =>
        {
            entity.HasIndex(x => x.EntryId);
            entity.Property(x => x.SignalType).HasMaxLength(80);
        });

        builder.Entity<EntryReview>(entity =>
        {
            entity.HasIndex(x => x.EntryId);
            entity.Property(x => x.Decision).HasConversion<string>().HasMaxLength(80);
        });

        builder.Entity<Draw>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.DrawReference }).IsUnique();
            entity.HasIndex(x => new { x.CompetitionId, x.Status });
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(60);
            entity.Property(x => x.DrawReference).HasMaxLength(80);
            entity.Property(x => x.AlgorithmVersion).HasMaxLength(80);
            entity.Property(x => x.EntryPoolHash).HasMaxLength(128);
            entity.Property(x => x.ConfigurationHash).HasMaxLength(128);
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<DrawEntrySnapshot>(entity =>
        {
            entity.HasIndex(x => new { x.DrawId, x.EntryId }).IsUnique();
            entity.Property(x => x.EntryReference).HasMaxLength(80);
            entity.Property(x => x.ParticipantReferenceHash).HasMaxLength(128);
            entity.Property(x => x.SnapshotHash).HasMaxLength(128);
            entity.HasOne(x => x.Draw).WithMany().HasForeignKey(x => x.DrawId);
        });

        builder.Entity<DrawResult>(entity =>
        {
            entity.HasIndex(x => new { x.DrawId, x.EntryId }).IsUnique();
            entity.Property(x => x.ResultType).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.SelectionProofHash).HasMaxLength(128);
            entity.HasOne(x => x.Draw).WithMany().HasForeignKey(x => x.DrawId);
        });

        builder.Entity<DrawCertificate>(entity =>
        {
            entity.HasIndex(x => x.DrawId).IsUnique();
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.StorageKey).HasMaxLength(500);
            entity.Property(x => x.Sha256Hash).HasMaxLength(128);
            entity.HasOne(x => x.Draw).WithMany().HasForeignKey(x => x.DrawId);
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<WinnerClaim>(entity =>
        {
            entity.HasIndex(x => x.DrawResultId).IsUnique();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(80);
        });

        builder.Entity<WinnerContactAttempt>(entity =>
        {
            entity.HasIndex(x => x.WinnerClaimId);
        });

        builder.Entity<ExportJob>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.CreatedAt });
            entity.Property(x => x.ExportType).HasMaxLength(80);
            entity.Property(x => x.Format).HasMaxLength(40);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<Plan>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.Code).HasMaxLength(80);
        });

        builder.Entity<TenantSubscription>(entity =>
        {
            entity.HasIndex(x => x.TenantId);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<BillingWebhookEvent>(entity =>
        {
            entity.HasIndex(x => new { x.Provider, x.ExternalEventId }).IsUnique();
            entity.Property(x => x.Provider).HasMaxLength(40);
            entity.Property(x => x.ExternalEventId).HasMaxLength(160);
            entity.Property(x => x.EventType).HasMaxLength(100);
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<FeatureFlag>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(80);
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.Property(x => x.Environment).HasMaxLength(40);
        });

        builder.Entity<TenantFeatureOverride>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.FeatureFlagId }).IsUnique();
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<Asset>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.StorageKey }).IsUnique();
            entity.Property(x => x.StorageKey).HasMaxLength(500);
            entity.Property(x => x.MimeType).HasMaxLength(160);
            entity.Property(x => x.Sha256Hash).HasMaxLength(128);
            entity.HasQueryFilter(x => x.DeletedAt == null && (!tenantContext.IsResolved || x.TenantId == tenantContext.TenantId));
        });

        builder.Entity<DataRetentionPolicy>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.CompetitionId }).IsUnique();
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<PrivacyRequest>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.Reference }).IsUnique();
            entity.Property(x => x.Reference).HasMaxLength(80);
            entity.Property(x => x.RequestType).HasMaxLength(40);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.TokenHash).HasMaxLength(128);
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<WebhookEndpoint>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.Url }).IsUnique();
            entity.Property(x => x.Url).HasMaxLength(500);
            entity.Property(x => x.SecretHash).HasMaxLength(128);
            entity.Property(x => x.SecretCiphertext).HasMaxLength(1000);
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<WebhookEvent>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.CreatedAt });
            entity.Property(x => x.EventType).HasMaxLength(120);
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<WebhookDelivery>(entity =>
        {
            entity.HasIndex(x => new { x.WebhookEventId, x.WebhookEndpointId });
            entity.Property(x => x.Status).HasMaxLength(40);
        });

        builder.Entity<NotificationMessage>(entity =>
        {
            entity.HasIndex(x => new { x.Status, x.AvailableAt });
            entity.HasIndex(x => new { x.TenantId, x.CreatedAt });
            entity.HasIndex(x => new { x.TenantId, x.DeduplicationKey });
            entity.Property(x => x.Channel).HasMaxLength(20);
            entity.Property(x => x.Recipient).HasMaxLength(320);
            entity.Property(x => x.Subject).HasMaxLength(500);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.DeduplicationKey).HasMaxLength(180);
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<AuditEvent>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.OccurredAt });
            entity.Property(x => x.ActorType).HasMaxLength(80);
            entity.Property(x => x.Action).HasMaxLength(160);
            entity.Property(x => x.EntityType).HasMaxLength(120);
            entity.Property(x => x.CorrelationId).HasMaxLength(120);
            entity.HasQueryFilter(x => !tenantContext.IsResolved || x.TenantId == tenantContext.TenantId);
        });

        builder.Entity<UserSession>(entity =>
        {
            entity.HasIndex(x => x.RefreshTokenHash).IsUnique();
            entity.Property(x => x.RefreshTokenHash).HasMaxLength(128);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });
    }

    public override int SaveChanges()
    {
        GuardAuditMutation();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        GuardAuditMutation();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void GuardAuditMutation()
    {
        foreach (var entry in ChangeTracker.Entries<AuditEvent>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("Audit events are append-only.");
            }
        }
    }
}
