using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneCompetitions.Application.Auth;
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
        builder.UseSetting("DATABASE_PROVIDER", "sqlite");
        builder.UseSetting("DATABASE_CONNECTION_STRING", $"Data Source={_databasePath}");
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT_SIGNING_KEY"] = "testing-signing-key-with-more-than-32-characters",
                ["PLATFORM_BASE_DOMAIN"] = "competitions.local",
                ["DATABASE_PROVIDER"] = "sqlite",
                ["DATABASE_CONNECTION_STRING"] = $"Data Source={_databasePath}"
            });
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

        await EnsureUserAsync(userManager, db, "admin@onecompetitions.local", true, one.Id, true);
        await EnsureUserAsync(userManager, db, "owner@one.local", false, one.Id, false);
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
}
