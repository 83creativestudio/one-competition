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

namespace OneCompetitions.Security.Tests;

public sealed class TestApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"one-competitions-security-{Guid.NewGuid():N}.db");

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

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "ONE. Digital",
            LegalName = "ONE. Digital",
            Slug = "one-digital",
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        if (!await db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Slug == tenant.Slug))
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync("owner@one.local") is null)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "owner@one.local",
                Email = "owner@one.local",
                EmailConfirmed = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await userManager.CreateAsync(user, "DevelopmentOnly!ChangeMe123");
            db.TenantUsers.Add(new TenantUser
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserId = user.Id,
                Role = TenantRole.TenantOwner,
                Status = TenantUserStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
