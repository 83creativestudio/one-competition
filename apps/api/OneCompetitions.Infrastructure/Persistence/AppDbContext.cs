using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Domain.Auditing;
using OneCompetitions.Domain.Branding;
using OneCompetitions.Domain.Tenants;
using OneCompetitions.Infrastructure.Identity;

namespace OneCompetitions.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ITenantContext tenantContext)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<TenantDomain> TenantDomains => Set<TenantDomain>();
    public DbSet<BrandProfile> BrandProfiles => Set<BrandProfile>();
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
