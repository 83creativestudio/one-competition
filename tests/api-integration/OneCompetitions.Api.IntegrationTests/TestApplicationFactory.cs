using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.SocialAuth;
using OneCompetitions.Domain.Branding;
using OneCompetitions.Domain.Billing;
using OneCompetitions.Domain.Features;
using OneCompetitions.Domain.Tenants;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Api.IntegrationTests;

public sealed class TestApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"one-competitions-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("JWT_SIGNING_KEY", "testing-signing-key-with-more-than-32-characters");
        builder.UseSetting("PLATFORM_BASE_DOMAIN", "competitions.local");
        builder.UseSetting("PLATFORM_AUTH_DOMAIN", "auth.competitions.local");
        builder.UseSetting("DATABASE_PROVIDER", "sqlite");
        builder.UseSetting("DATABASE_CONNECTION_STRING", $"Data Source={_databasePath}");
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT_SIGNING_KEY"] = "testing-signing-key-with-more-than-32-characters",
                ["PLATFORM_BASE_DOMAIN"] = "competitions.local",
                ["PLATFORM_AUTH_DOMAIN"] = "auth.competitions.local",
                ["DATABASE_PROVIDER"] = "sqlite",
                ["DATABASE_CONNECTION_STRING"] = $"Data Source={_databasePath}"
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISocialAuthProviderRegistry>();
            services.AddSingleton<ISocialAuthProviderRegistry, FakeSocialAuthProviderRegistry>();
        });
    }

    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var one = await EnsureTenantAsync(db, "ONE. Digital", "one-digital");
        var omega = await EnsureTenantAsync(db, "Omega TV", "omega-tv");
        await EnsureBrandAsync(db, one.Id, "ONE. Digital");
        await EnsureBrandAsync(db, omega.Id, "Omega TV");
        if (!await db.Plans.AnyAsync())
        {
            db.Plans.Add(new Plan
            {
                Id = Guid.NewGuid(), Name = "Test", Code = "test", MonthlyPrice = 0, AnnualPrice = 0,
                FeatureConfigurationJson = "{\"MaximumActiveCompetitions\":100,\"MaximumCustomDomains\":100,\"AllowCustomDomain\":true,\"AllowExports\":true,\"AllowDrawCertificate\":true,\"AllowWebhooks\":true}",
                IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
            });
        }
        if (!await db.FeatureFlags.AnyAsync(x => x.Code == "SocialLogin"))
            db.FeatureFlags.Add(new FeatureFlag { Id = Guid.NewGuid(), Code = "SocialLogin", Name = "Social login", IsEnabled = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });

        await EnsureUserAsync(userManager, db, "admin@onecompetitions.local", true, one.Id, true);
        await EnsureUserAsync(userManager, db, "owner@one.local", false, one.Id, true);
        await EnsureUserAsync(userManager, db, "owner@omega.local", false, omega.Id, false);

        await db.SaveChangesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    private static async Task<Tenant> EnsureTenantAsync(AppDbContext db, string name, string slug)
    {
        var tenant = await db.Tenants.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Slug == slug);
        if (tenant is not null)
        {
            return tenant;
        }

        tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            LegalName = name,
            Slug = slug,
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Tenants.Add(tenant);
        db.TenantDomains.Add(new TenantDomain
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Hostname = $"{slug}.competitions.local",
            DomainType = TenantDomainType.PlatformSubdomain,
            Status = TenantDomainStatus.Active,
            IsPrimary = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return tenant;
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> userManager, AppDbContext db, string email, bool platformAdmin, Guid tenantId, bool tenantOwner)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = email,
                IsPlatformUser = platformAdmin,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            var result = await userManager.CreateAsync(user, "DevelopmentOnly!ChangeMe123");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
            }
        }

        if (platformAdmin && !await userManager.IsInRoleAsync(user, AppRoles.PlatformAdministrator))
        {
            await userManager.AddToRoleAsync(user, AppRoles.PlatformAdministrator);
        }

        if (!await db.TenantUsers.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.UserId == user.Id))
        {
            db.TenantUsers.Add(new TenantUser
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = user.Id,
                Role = tenantOwner ? TenantRole.TenantOwner : TenantRole.Viewer,
                Status = TenantUserStatus.Active,
                AcceptedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    private static async Task EnsureBrandAsync(AppDbContext db, Guid tenantId, string name)
    {
        if (await db.BrandProfiles.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Name == name))
        {
            return;
        }

        db.BrandProfiles.Add(new BrandProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            PrimaryColor = "#0f766e",
            SecondaryColor = "#111827",
            AccentColor = "#c2410c",
            BackgroundColor = "#ffffff",
            TextColor = "#111827",
            HeadingFont = "Inter",
            BodyFont = "Inter",
            ButtonStyle = "Solid",
            BorderRadius = 6,
            FooterText = $"{name} competitions",
            ShowPoweredBy = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }
}
