using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneCompetitions.Application.Auditing;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Branding;
using OneCompetitions.Application.Domains;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;
using OneCompetitions.Infrastructure.Services;
using OneCompetitions.Infrastructure.Tenancy;

namespace OneCompetitions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantContextSetter>(provider => provider.GetRequiredService<TenantContext>());

        services.AddDbContext<AppDbContext>(options =>
        {
            var provider = configuration["DATABASE_PROVIDER"] ?? "postgres";
            if (string.Equals(provider, "sqlite", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(configuration["DATABASE_CONNECTION_STRING"] ?? "Data Source=one-competitions-test.db");
                return;
            }

            options.UseNpgsql(configuration["DATABASE_CONNECTION_STRING"]
                ?? configuration.GetConnectionString("Database")
                ?? "Host=localhost;Port=5432;Database=one_competitions;Username=one;Password=one_dev_password");
        });

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITenantAccessService, TenantAccessService>();
        services.AddScoped<ITenantDomainService, TenantDomainService>();
        services.AddScoped<IDomainVerificationProvider, DevelopmentDomainVerificationProvider>();
        services.AddScoped<ISslProvisioningProvider, UnavailableSslProvisioningProvider>();
        services.AddScoped<IBrandProfileService, BrandProfileService>();
        services.AddScoped<IAuditLogger, AuditLogger>();

        return services;
    }
}
