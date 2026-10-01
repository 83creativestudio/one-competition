using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OneCompetitions.Application.Auth;
using OneCompetitions.Domain.Billing;
using OneCompetitions.Domain.Branding;
using OneCompetitions.Domain.Campaigns;
using OneCompetitions.Domain.Competitions;
using OneCompetitions.Domain.Consents;
using OneCompetitions.Domain.Entries;
using OneCompetitions.Domain.Features;
using OneCompetitions.Domain.Participants;
using OneCompetitions.Domain.Tenants;
using OneCompetitions.Infrastructure.Identity;

namespace OneCompetitions.Infrastructure.Persistence;

public static class DevelopmentSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IHostEnvironment environment, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (string.Equals(configuration["DATABASE_PROVIDER"], "sqlite", StringComparison.OrdinalIgnoreCase))
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        else
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
        var omegaTv = await EnsureTenantAsync(dbContext, "Omega TV", "omega-tv", cancellationToken);
        var restaurant = await EnsureTenantAsync(dbContext, "Demo Restaurant", "demo-restaurant", cancellationToken);
        var retail = await EnsureTenantAsync(dbContext, "Demo Retail Brand", "demo-retail-brand", cancellationToken);
        await EnsureBrandProfileAsync(dbContext, oneDigital.Id, "ONE. Digital", "#0f766e", "#111827", "#c2410c", cancellationToken);
        await EnsureBrandProfileAsync(dbContext, omegaTv.Id, "Omega TV", "#1d4ed8", "#111827", "#dc2626", cancellationToken);
        await EnsureBrandProfileAsync(dbContext, restaurant.Id, "Demo Restaurant", "#166534", "#292524", "#ca8a04", cancellationToken);
        await EnsureBrandProfileAsync(dbContext, retail.Id, "Demo Retail Brand", "#7c3aed", "#111827", "#db2777", cancellationToken);
        await EnsurePlansAndFlagsAsync(dbContext, cancellationToken);
        await EnsureCompetitionSeedAsync(dbContext, oneDigital.Id, admin.Id, "Win an iPhone", "win-an-iphone", cancellationToken);
        await EnsureCompetitionSeedAsync(dbContext, omegaTv.Id, admin.Id, "Live TV Prize Draw", "live-tv-prize-draw", cancellationToken);
        await EnsureCompetitionSeedAsync(dbContext, restaurant.Id, admin.Id, "Summer Restaurant Giveaway", "summer-restaurant-giveaway", cancellationToken);
        await EnsureCompetitionSeedAsync(dbContext, retail.Id, admin.Id, "Shopping Voucher Competition", "shopping-voucher-competition", cancellationToken);

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

    private static async Task EnsureBrandProfileAsync(AppDbContext dbContext, Guid tenantId, string name, string primaryColor, string secondaryColor, string accentColor, CancellationToken cancellationToken)
    {
        if (await dbContext.BrandProfiles.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Name == name, cancellationToken))
        {
            return;
        }

        dbContext.BrandProfiles.Add(new BrandProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            PrimaryColor = primaryColor,
            SecondaryColor = secondaryColor,
            AccentColor = accentColor,
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

    private static async Task EnsurePlansAndFlagsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!await dbContext.Plans.AnyAsync(x => x.Code == "starter", cancellationToken))
        {
            dbContext.Plans.Add(new Plan
            {
                Id = Guid.NewGuid(),
                Name = "Starter",
                Code = "starter",
                MonthlyPrice = 49,
                AnnualPrice = 490,
                FeatureConfigurationJson = "{\"MaximumActiveCompetitions\":10,\"MaximumCustomDomains\":3,\"MaximumTeamMembers\":10,\"AllowCustomDomain\":true,\"AllowExports\":true,\"AllowDrawCertificate\":true,\"AllowWebhooks\":true}",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        foreach (var code in new[] { "CustomDomains", "SocialLogin", "Referrals", "ReceiptUploads", "InstantWin", "TvMode", "DrawCertificates", "AdvancedFraud", "PublicDirectory", "ResellerMode" })
        {
            if (!await dbContext.FeatureFlags.AnyAsync(x => x.Code == code, cancellationToken))
            {
                dbContext.FeatureFlags.Add(new FeatureFlag
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Name = code,
                    IsEnabled = code is "CustomDomains" or "SocialLogin" or "DrawCertificates" or "AdvancedFraud",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }
        }
        var socialLogin = dbContext.FeatureFlags.Local.SingleOrDefault(x => x.Code == "SocialLogin")
            ?? await dbContext.FeatureFlags.SingleAsync(x => x.Code == "SocialLogin", cancellationToken);
        socialLogin.IsEnabled = true;
        socialLogin.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static async Task EnsureCompetitionSeedAsync(AppDbContext dbContext, Guid tenantId, Guid adminUserId, string name, string slug, CancellationToken cancellationToken)
    {
        if (await dbContext.Competitions.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Slug == slug, cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var competition = new Competition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Slug = slug,
            Description = "Development seed competition",
            Status = CompetitionStatus.Live,
            StartsAt = now.AddDays(-1),
            EndsAt = now.AddDays(14),
            NumberOfWinners = 1,
            NumberOfReserveWinners = 1,
            RequiresManualApproval = true,
            PublishedByUserId = adminUserId,
            PublishedAt = now,
            CreatedByUserId = adminUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.Competitions.Add(competition);

        dbContext.CompetitionFields.AddRange(
            new CompetitionField { Id = Guid.NewGuid(), CompetitionId = competition.Id, FieldKey = "email", FieldType = CompetitionFieldType.Email, Label = "Email", IsRequired = true, DisplayOrder = 1, CreatedAt = now, UpdatedAt = now },
            new CompetitionField { Id = Guid.NewGuid(), CompetitionId = competition.Id, FieldKey = "phone", FieldType = CompetitionFieldType.Phone, Label = "Phone", IsRequired = false, DisplayOrder = 2, CreatedAt = now, UpdatedAt = now });

        dbContext.CompetitionRulesVersions.Add(new CompetitionRulesVersion
        {
            Id = Guid.NewGuid(),
            CompetitionId = competition.Id,
            VersionNumber = 1,
            LanguageCode = "en",
            Title = $"{name} rules",
            Content = "Development-only rules for local testing.",
            ContentHash = "development-seed",
            EffectiveAt = now,
            CreatedByUserId = adminUserId,
            CreatedAt = now
        });

        dbContext.CompetitionPages.Add(new CompetitionPage
        {
            Id = Guid.NewGuid(),
            CompetitionId = competition.Id,
            LanguageCode = "en",
            Status = "Published",
            Title = name,
            SeoTitle = name,
            SeoDescription = "Enter the competition",
            LayoutJson = "{\"schemaVersion\":1,\"blocks\":[{\"id\":\"hero-1\",\"type\":\"hero\",\"settings\":{\"headline\":\"Enter now\"}},{\"id\":\"entry-1\",\"type\":\"EntryForm\",\"settings\":{}}]}",
            PublishedVersion = 1,
            CreatedAt = now,
            UpdatedAt = now,
            PublishedAt = now
        });

        var terms = new ConsentDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CompetitionId = competition.Id,
            ConsentType = ConsentType.CompetitionTerms,
            LanguageCode = "en",
            Text = "I accept the competition terms.",
            IsRequired = true,
            CreatedAt = now
        };
        var marketing = new ConsentDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CompetitionId = competition.Id,
            ConsentType = ConsentType.MarketingEmail,
            LanguageCode = "en",
            Text = "I agree to receive marketing email.",
            IsRequired = false,
            CreatedAt = now
        };
        dbContext.ConsentDefinitions.AddRange(terms, marketing);

        var source = new CampaignSource
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CompetitionId = competition.Id,
            Name = "Website",
            SourceType = CampaignSourceType.Website,
            Code = "WEB",
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.CampaignSources.Add(source);

        for (var i = 1; i <= 3; i++)
        {
            var participant = new Participant
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PrimaryEmail = $"participant{i}@example.local",
                FirstName = "Demo",
                LastName = $"Participant {i}",
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.Participants.Add(participant);
            dbContext.CompetitionEntries.Add(new CompetitionEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CompetitionId = competition.Id,
                ParticipantId = participant.Id,
                EntryReference = $"ENT-{now:yyyy}-{i:000000}",
                Status = CompetitionEntryStatus.Approved,
                EligibilityStatus = EligibilityStatus.Eligible,
                RiskLevel = RiskLevel.Low,
                EntrySourceId = source.Id,
                SubmittedAt = now.AddMinutes(i),
                ApprovedAt = now.AddMinutes(i + 1),
                CreatedAt = now,
                UpdatedAt = now
            });
        }
    }
}
