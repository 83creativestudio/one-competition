using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OneCompetitions.Application.Auth;
using OneCompetitions.Domain.Tenants;
using OneCompetitions.Infrastructure.Identity;

namespace OneCompetitions.Infrastructure.Persistence;

public static class DevelopmentSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IHostEnvironment environment, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var password = configuration["DEV_ADMIN_PASSWORD"]
            ?? configuration["ONECOMPETITIONS_DEMO_PASSWORD"]
            ?? "DevelopmentOnly!ChangeMe123";

        var admin = await userManager.FindByEmailAsync("admin@onecompetitions.local");
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UserName = "admin@onecompetitions.local",
                Email = "admin@onecompetitions.local",
                EmailConfirmed = true,
                DisplayName = "Platform Administrator",
                IsPlatformUser = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(admin, AppRoles.PlatformAdministrator))
        {
            await userManager.AddToRoleAsync(admin, AppRoles.PlatformAdministrator);
        }

        var oneDigital = await EnsureTenantAsync(dbContext, "ONE. Digital", "one-digital", cancellationToken);
        await EnsureTenantAsync(dbContext, "Omega TV", "omega-tv", cancellationToken);
        await EnsureTenantAsync(dbContext, "Demo Restaurant", "demo-restaurant", cancellationToken);
        await EnsureTenantAsync(dbContext, "Demo Retail Brand", "demo-retail-brand", cancellationToken);

        if (!await dbContext.TenantUsers.IgnoreQueryFilters().AnyAsync(x => x.TenantId == oneDigital.Id && x.UserId == admin.Id, cancellationToken))
        {
            dbContext.TenantUsers.Add(new TenantUser
            {
                Id = Guid.NewGuid(),
                TenantId = oneDigital.Id,
                UserId = admin.Id,
                Role = TenantRole.TenantOwner,
                Status = TenantUserStatus.Active,
                AcceptedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Tenant> EnsureTenantAsync(AppDbContext dbContext, string name, string slug, CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Slug == slug, cancellationToken);
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
            DefaultLanguage = "en",
            TimeZone = "Asia/Nicosia",
            CountryCode = "CY",
            Currency = "EUR",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Tenants.Add(tenant);
        dbContext.TenantDomains.Add(new TenantDomain
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

        return tenant;
    }
}
