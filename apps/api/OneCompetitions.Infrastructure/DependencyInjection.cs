using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OneCompetitions.Application.Auditing;
using OneCompetitions.Application.Auth;
using OneCompetitions.Application.Billing;
using OneCompetitions.Application.Branding;
using OneCompetitions.Application.Campaigns;
using OneCompetitions.Application.Competitions;
using OneCompetitions.Application.Domains;
using OneCompetitions.Application.Draws;
using OneCompetitions.Application.Entries;
using OneCompetitions.Application.Exports;
using OneCompetitions.Application.Jobs;
using OneCompetitions.Application.Locking;
using OneCompetitions.Application.Notifications;
using OneCompetitions.Application.Storage;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Application.Winners;
using OneCompetitions.Application.Webhooks;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Health;
using OneCompetitions.Infrastructure.Persistence;
using OneCompetitions.Infrastructure.Services;
using OneCompetitions.Infrastructure.Tenancy;
using StackExchange.Redis;

namespace OneCompetitions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
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
        services.AddScoped<IBrandProfileService, BrandProfileService>();
        services.AddScoped<ICompetitionService, CompetitionService>();
        services.AddScoped<IEntryService, EntryService>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IDrawService, DrawService>();
        services.AddScoped<IWinnerService, WinnerService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<INotificationQueue, NotificationQueue>();
        services.AddScoped<IWebhookService, WebhookService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IPlanLimitService, PlanLimitService>();
        services.AddScoped<IDrawCertificateService, DrawCertificateService>();
        services.AddScoped<IPlatformJobProcessor, PlatformJobProcessor>();
        services.AddSingleton<SecretProtector>();
        services.AddHttpClient();
        services.AddHttpClient<HttpVirusScanner>();
        services.AddHttpClient<HttpSmsProvider>();
        services.AddHttpClient<CloudflareSslProvisioningProvider>();
        services.AddHttpClient("webhooks", client => client.Timeout = TimeSpan.FromSeconds(15));

        var isDevelopment = environment.IsDevelopment() || environment.IsEnvironment("Testing");
        RegisterDomainProviders(services, configuration, isDevelopment);
        RegisterStorage(services, configuration, isDevelopment);
        RegisterMessaging(services, configuration, isDevelopment);
        RegisterLocks(services, configuration, isDevelopment);
        var healthChecks = services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
            .AddCheck<ObjectStorageHealthCheck>("object-storage", tags: ["ready"]);
        if (!isDevelopment || string.Equals(configuration["DISTRIBUTED_LOCK_PROVIDER"], "redis", StringComparison.OrdinalIgnoreCase))
            healthChecks.AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);

        return services;
    }

    private static void RegisterDomainProviders(IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        var dnsProvider = configuration["DOMAIN_VERIFICATION_PROVIDER"] ?? (isDevelopment ? "development" : "dns");
        if (dnsProvider.Equals("development", StringComparison.OrdinalIgnoreCase))
        {
            if (!isDevelopment) throw new InvalidOperationException("Development DNS verification cannot run outside development or testing.");
            services.AddScoped<IDomainVerificationProvider, DevelopmentDomainVerificationProvider>();
        }
        else if (dnsProvider.Equals("dns", StringComparison.OrdinalIgnoreCase)) services.AddScoped<IDomainVerificationProvider, DnsDomainVerificationProvider>();
        else throw new InvalidOperationException($"Unsupported DOMAIN_VERIFICATION_PROVIDER '{dnsProvider}'.");

        var sslProvider = configuration["SSL_PROVISIONING_PROVIDER"] ?? (isDevelopment ? "development" : "cloudflare");
        if (sslProvider.Equals("development", StringComparison.OrdinalIgnoreCase))
        {
            if (!isDevelopment) throw new InvalidOperationException("Development SSL provisioning cannot run outside development or testing.");
            services.AddScoped<ISslProvisioningProvider, DevelopmentSslProvisioningProvider>();
        }
        else if (sslProvider.Equals("cloudflare", StringComparison.OrdinalIgnoreCase)) services.AddScoped<ISslProvisioningProvider>(x => x.GetRequiredService<CloudflareSslProvisioningProvider>());
        else throw new InvalidOperationException($"Unsupported SSL_PROVISIONING_PROVIDER '{sslProvider}'.");
    }

    private static void RegisterStorage(IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        var provider = configuration["OBJECT_STORAGE_PROVIDER"] ?? (isDevelopment ? "local" : "s3");
        if (provider.Equals("local", StringComparison.OrdinalIgnoreCase))
        {
            if (!isDevelopment) throw new InvalidOperationException("Local object storage cannot run outside development or testing.");
            services.AddSingleton<IObjectStorage, LocalObjectStorage>();
            services.AddScoped<IVirusScanner, DevelopmentVirusScanner>();
            return;
        }

        if (!provider.Equals("s3", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"Unsupported OBJECT_STORAGE_PROVIDER '{provider}'.");
        var accessKey = configuration["OBJECT_STORAGE_ACCESS_KEY"];
        var secretKey = configuration["OBJECT_STORAGE_SECRET_KEY"];
        var endpoint = configuration["OBJECT_STORAGE_ENDPOINT"];
        var storageConfig = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = configuration.GetValue("OBJECT_STORAGE_FORCE_PATH_STYLE", false),
            AuthenticationRegion = configuration["OBJECT_STORAGE_REGION"] ?? "us-east-1"
        };
        services.AddSingleton<IAmazonS3>(_ => !string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey)
            ? new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), storageConfig)
            : new AmazonS3Client(storageConfig));
        services.AddSingleton<IObjectStorage, S3ObjectStorage>();
        var scanner = configuration["VIRUS_SCANNER_PROVIDER"] ?? (isDevelopment ? "development" : "http");
        if (scanner.Equals("development", StringComparison.OrdinalIgnoreCase))
        {
            if (!isDevelopment) throw new InvalidOperationException("Development malware scanning cannot run outside development or testing.");
            services.AddScoped<IVirusScanner, DevelopmentVirusScanner>();
        }
        else if (scanner.Equals("http", StringComparison.OrdinalIgnoreCase)) services.AddScoped<IVirusScanner>(x => x.GetRequiredService<HttpVirusScanner>());
        else throw new InvalidOperationException($"Unsupported VIRUS_SCANNER_PROVIDER '{scanner}'.");
    }

    private static void RegisterMessaging(IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        var email = configuration["EMAIL_PROVIDER"] ?? (isDevelopment ? "development" : "smtp");
        if (email.Equals("development", StringComparison.OrdinalIgnoreCase))
        {
            if (!isDevelopment) throw new InvalidOperationException("Development email provider cannot run outside development or testing.");
            services.AddScoped<IEmailProvider, DevelopmentEmailProvider>();
        }
        else if (email.Equals("smtp", StringComparison.OrdinalIgnoreCase)) services.AddScoped<IEmailProvider, SmtpEmailProvider>();
        else throw new InvalidOperationException($"Unsupported EMAIL_PROVIDER '{email}'.");

        var sms = configuration["SMS_PROVIDER"] ?? (isDevelopment ? "development" : "http");
        if (sms.Equals("development", StringComparison.OrdinalIgnoreCase))
        {
            if (!isDevelopment) throw new InvalidOperationException("Development SMS provider cannot run outside development or testing.");
            services.AddScoped<ISmsProvider, DevelopmentSmsProvider>();
        }
        else if (sms.Equals("http", StringComparison.OrdinalIgnoreCase)) services.AddScoped<ISmsProvider>(x => x.GetRequiredService<HttpSmsProvider>());
        else throw new InvalidOperationException($"Unsupported SMS_PROVIDER '{sms}'.");
    }

    private static void RegisterLocks(IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        var provider = configuration["DISTRIBUTED_LOCK_PROVIDER"] ?? (isDevelopment ? "memory" : "redis");
        if (provider.Equals("memory", StringComparison.OrdinalIgnoreCase))
        {
            if (!isDevelopment) throw new InvalidOperationException("In-memory distributed locks cannot run outside development or testing.");
            services.AddSingleton<IDistributedLockProvider, InMemoryDistributedLockProvider>();
            return;
        }
        if (!provider.Equals("redis", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"Unsupported DISTRIBUTED_LOCK_PROVIDER '{provider}'.");
        var connection = configuration["REDIS_CONNECTION_STRING"] ?? throw new InvalidOperationException("REDIS_CONNECTION_STRING is required.");
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connection));
        services.AddSingleton<IDistributedLockProvider, RedisDistributedLockProvider>();
    }
}
