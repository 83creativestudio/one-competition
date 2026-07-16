using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneCompetitions.Application.Auditing;
using OneCompetitions.Application.Billing;
using OneCompetitions.Application.Competitions;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Contracts.Competitions;
using OneCompetitions.Contracts.Entries;
using OneCompetitions.Domain.Competitions;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class CompetitionService(
    AppDbContext dbContext,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    IAuditLogger auditLogger,
    IPlanLimitService planLimits)
    : TenantScopedServiceBase(dbContext, tenantContext, userManager), ICompetitionService
{
    public async Task<IReadOnlyList<CompetitionResponse>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        if (DbContext.Database.IsSqlite())
            return (await DbContext.Competitions.Take(500).ToListAsync(cancellationToken)).OrderByDescending(x => x.CreatedAt).Select(ToResponse).ToList();
        return await DbContext.Competitions.OrderByDescending(x => x.CreatedAt).Take(500).Select(x => ToResponse(x)).ToListAsync(cancellationToken);
    }

    public async Task<CompetitionResponse> CreateAsync(Guid userId, CreateCompetitionRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        await planLimits.EnsureAllowedAsync(TenantContext.TenantId, "MaximumActiveCompetitions", 1, cancellationToken);
        ValidateCompetition(request.Name, request.Slug, request.StartsAt, request.EndsAt, request.PerParticipantEntryLimit, request.NumberOfWinners);

        var now = DateTimeOffset.UtcNow;
        var competition = new Competition
        {
            Id = Guid.NewGuid(),
            TenantId = TenantContext.TenantId,
            Name = request.Name.Trim(),
            Slug = NormalizeSlug(request.Slug),
            Description = request.Description,
            StartsAt = request.StartsAt.ToUniversalTime(),
            EndsAt = request.EndsAt.ToUniversalTime(),
            EntryLimit = request.EntryLimit,
            PerParticipantEntryLimit = request.PerParticipantEntryLimit,
            NumberOfWinners = request.NumberOfWinners,
            NumberOfReserveWinners = request.NumberOfReserveWinners,
            RequiresManualApproval = request.RequiresManualApproval,
            MinimumAge = request.MinimumAge,
            AllowedCountryCodesJson = JsonSerializer.Serialize(NormalizeCountries(request.AllowedCountryCodes)),
            RequiresEmailVerification = request.RequiresEmailVerification,
            RequiresPhoneVerification = request.RequiresPhoneVerification,
            CreatedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };

        DbContext.Competitions.Add(competition);
        DbContext.CompetitionPages.Add(DefaultPage(competition, now));
        await DbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync(userId, "competition.created", "Competition", competition.Id, cancellationToken);
        return ToResponse(competition);
    }

    public async Task<CompetitionResponse?> GetAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        return await DbContext.Competitions.Where(x => x.Id == competitionId).Select(x => ToResponse(x)).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CompetitionResponse> UpdateAsync(Guid userId, Guid competitionId, UpdateCompetitionRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        ValidateCompetition(request.Name, "unchanged", request.StartsAt, request.EndsAt, request.PerParticipantEntryLimit, request.NumberOfWinners);

        var competition = await DbContext.Competitions.SingleOrDefaultAsync(x => x.Id == competitionId, cancellationToken)
            ?? throw new InvalidOperationException("Competition was not found.");

        if (competition.PublishedAt is not null)
        {
            await CreateVersionAsync(competition, userId, "Configuration changed after publication.", cancellationToken);
        }

        competition.Name = request.Name.Trim();
        competition.Description = request.Description;
        competition.StartsAt = request.StartsAt.ToUniversalTime();
        competition.EndsAt = request.EndsAt.ToUniversalTime();
        competition.EntryLimit = request.EntryLimit;
        competition.PerParticipantEntryLimit = request.PerParticipantEntryLimit;
        competition.NumberOfWinners = request.NumberOfWinners;
        competition.NumberOfReserveWinners = request.NumberOfReserveWinners;
        competition.RequiresManualApproval = request.RequiresManualApproval;
        competition.MinimumAge = request.MinimumAge;
        competition.AllowedCountryCodesJson = JsonSerializer.Serialize(NormalizeCountries(request.AllowedCountryCodes));
        competition.RequiresEmailVerification = request.RequiresEmailVerification;
        competition.RequiresPhoneVerification = request.RequiresPhoneVerification;
        competition.UpdatedAt = DateTimeOffset.UtcNow;

        await DbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync(userId, "competition.updated", "Competition", competition.Id, cancellationToken);
        return ToResponse(competition);
    }

    public async Task<CompetitionResponse> PublishAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var competition = await DbContext.Competitions.SingleOrDefaultAsync(x => x.Id == competitionId, cancellationToken)
            ?? throw new InvalidOperationException("Competition was not found.");

        if (competition.CompetitionType != CompetitionType.StandardDraw)
        {
            throw new InvalidOperationException("Only standard draw competitions can be published in this release.");
        }

        var now = DateTimeOffset.UtcNow;
        competition.PublishedAt ??= now;
        competition.PublishedByUserId = userId;
        competition.Status = now < competition.StartsAt ? CompetitionStatus.Scheduled : CompetitionStatus.Live;
        competition.UpdatedAt = now;

        await CreateVersionAsync(competition, userId, "Published competition configuration.", cancellationToken);
        var page = await DbContext.CompetitionPages.SingleOrDefaultAsync(x => x.CompetitionId == competition.Id && x.LanguageCode == competition.DefaultLanguage, cancellationToken);
        if (page is not null)
        {
            page.Status = "Published";
            page.PublishedVersion += 1;
            page.PublishedAt = now;
            page.UpdatedAt = now;
        }

        await DbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync(userId, "competition.published", "Competition", competition.Id, cancellationToken);
        return ToResponse(competition);
    }

    public async Task<CompetitionResponse> CloseAsync(Guid userId, Guid competitionId, string? reason, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var competition = await DbContext.Competitions.SingleOrDefaultAsync(x => x.Id == competitionId, cancellationToken)
            ?? throw new InvalidOperationException("Competition was not found.");

        competition.Status = CompetitionStatus.Closed;
        competition.ClosedAt = DateTimeOffset.UtcNow;
        competition.UpdatedAt = DateTimeOffset.UtcNow;
        await DbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync(userId, "competition.closed", "Competition", competition.Id, cancellationToken, reason);
        return ToResponse(competition);
    }

    public async Task<IReadOnlyList<CompetitionVersionResponse>> VersionsAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        await EnsureCompetitionAsync(competitionId, cancellationToken);
        return await DbContext.CompetitionVersions
            .Where(x => x.CompetitionId == competitionId)
            .OrderByDescending(x => x.VersionNumber)
            .Select(x => new CompetitionVersionResponse(x.Id, x.CompetitionId, x.VersionNumber, x.ChangeSummary, x.CreatedAt, x.PublishedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CompetitionFieldResponse>> ListFieldsAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        await EnsureCompetitionAsync(competitionId, cancellationToken);
        return await DbContext.CompetitionFields.Where(x => x.CompetitionId == competitionId).OrderBy(x => x.DisplayOrder).Select(x => ToResponse(x)).ToListAsync(cancellationToken);
    }

    public async Task<CompetitionFieldResponse> AddFieldAsync(Guid userId, Guid competitionId, UpsertCompetitionFieldRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        await EnsureCompetitionAsync(competitionId, cancellationToken);
        var field = BuildField(competitionId, request, Guid.NewGuid());
        DbContext.CompetitionFields.Add(field);
        await DbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync(userId, "competition_field.created", "CompetitionField", field.Id, cancellationToken);
        return ToResponse(field);
    }

    public async Task<CompetitionFieldResponse> UpdateFieldAsync(Guid userId, Guid competitionId, Guid fieldId, UpsertCompetitionFieldRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var field = await DbContext.CompetitionFields.SingleOrDefaultAsync(x => x.Id == fieldId && x.CompetitionId == competitionId, cancellationToken)
            ?? throw new InvalidOperationException("Competition field was not found.");
        ApplyField(field, request);
        await DbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync(userId, "competition_field.updated", "CompetitionField", field.Id, cancellationToken);
        return ToResponse(field);
    }

    public async Task DeleteFieldAsync(Guid userId, Guid competitionId, Guid fieldId, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var field = await DbContext.CompetitionFields.SingleOrDefaultAsync(x => x.Id == fieldId && x.CompetitionId == competitionId, cancellationToken)
            ?? throw new InvalidOperationException("Competition field was not found.");
        DbContext.CompetitionFields.Remove(field);
        await DbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync(userId, "competition_field.deleted", "CompetitionField", field.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<CompetitionFieldResponse>> ReorderFieldsAsync(Guid userId, Guid competitionId, ReorderCompetitionFieldsRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var fields = await DbContext.CompetitionFields.Where(x => x.CompetitionId == competitionId).ToListAsync(cancellationToken);
        for (var i = 0; i < request.FieldIds.Count; i++)
        {
            var field = fields.SingleOrDefault(x => x.Id == request.FieldIds[i]);
            if (field is not null)
            {
                field.DisplayOrder = i + 1;
                field.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await DbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync(userId, "competition_field.reordered", "Competition", competitionId, cancellationToken);
        return fields.OrderBy(x => x.DisplayOrder).Select(ToResponse).ToList();
    }

    public async Task<RulesVersionResponse> AddRulesAsync(Guid userId, Guid competitionId, CreateRulesVersionRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var competition = await EnsureCompetitionAsync(competitionId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new InvalidOperationException("Rules content is required.");
        }

        var nextVersion = await DbContext.CompetitionRulesVersions.Where(x => x.CompetitionId == competitionId).Select(x => (int?)x.VersionNumber).MaxAsync(cancellationToken) ?? 0;
        var rules = new CompetitionRulesVersion
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId,
            VersionNumber = nextVersion + 1,
            LanguageCode = string.IsNullOrWhiteSpace(request.LanguageCode) ? competition.DefaultLanguage : request.LanguageCode.Trim().ToLowerInvariant(),
            Title = request.Title.Trim(),
            Content = request.Content,
            ContentHash = Hash(request.Content),
            EffectiveAt = request.EffectiveAt?.ToUniversalTime() ?? DateTimeOffset.UtcNow,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        DbContext.CompetitionRulesVersions.Add(rules);
        competition.RulesVersionId = rules.Id;
        competition.UpdatedAt = DateTimeOffset.UtcNow;
        await DbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync(userId, "competition_rules.created", "CompetitionRulesVersion", rules.Id, cancellationToken);
        return ToResponse(rules);
    }

    public async Task<CompetitionPageResponse> UpsertPageAsync(Guid userId, Guid competitionId, UpsertCompetitionPageRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        await EnsureCompetitionAsync(competitionId, cancellationToken);
        _ = JsonDocument.Parse(request.LayoutJson);

        var language = string.IsNullOrWhiteSpace(request.LanguageCode) ? "en" : request.LanguageCode.Trim().ToLowerInvariant();
        var page = await DbContext.CompetitionPages.SingleOrDefaultAsync(x => x.CompetitionId == competitionId && x.LanguageCode == language, cancellationToken);
        if (page is null)
        {
            page = new CompetitionPage
            {
                Id = Guid.NewGuid(),
                CompetitionId = competitionId,
                LanguageCode = language,
                CreatedAt = DateTimeOffset.UtcNow
            };
            DbContext.CompetitionPages.Add(page);
        }

        page.Title = request.Title.Trim();
        page.SeoTitle = request.SeoTitle;
        page.SeoDescription = request.SeoDescription;
        page.LayoutJson = request.LayoutJson;
        page.Status = "Draft";
        page.UpdatedAt = DateTimeOffset.UtcNow;
        await DbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync(userId, "competition_page.updated", "CompetitionPage", page.Id, cancellationToken);
        return ToResponse(page);
    }

    public async Task<PublicCompetitionResponse?> GetPublicAsync(string slug, CancellationToken cancellationToken)
    {
        if (!TenantContext.IsResolved)
        {
            return null;
        }

        var competition = await DbContext.Competitions.SingleOrDefaultAsync(x => x.Slug == NormalizeSlug(slug), cancellationToken);
        if (competition is null || competition.PublishedAt is null)
        {
            return null;
        }

        var fields = await DbContext.CompetitionFields.Where(x => x.CompetitionId == competition.Id).OrderBy(x => x.DisplayOrder).Select(x => ToResponse(x)).ToListAsync(cancellationToken);
        var consentVersions = await DbContext.ConsentDefinitions.Where(x => x.CompetitionId == competition.Id && x.LanguageCode == competition.DefaultLanguage).ToListAsync(cancellationToken);
        var consents = consentVersions.GroupBy(x => x.ConsentType).Select(x => x.OrderByDescending(v => v.Version).First())
            .Select(x => new PublicConsentResponse(x.Id, x.ConsentType.ToString(), x.LanguageCode, x.Text, x.IsRequired, x.Version)).ToList();
        var page = await DbContext.CompetitionPages.Where(x => x.CompetitionId == competition.Id && x.LanguageCode == competition.DefaultLanguage).Select(x => ToResponse(x)).SingleOrDefaultAsync(cancellationToken);
        return new PublicCompetitionResponse(competition.Id, competition.TenantId, TenantContext.TenantSlug, competition.Name, competition.Slug, competition.Status.ToString(), competition.StartsAt, competition.EndsAt,
            competition.MinimumAge, ParseCountries(competition.AllowedCountryCodesJson), competition.RequiresEmailVerification, competition.RequiresPhoneVerification, fields, consents, page);
    }

    private async Task<Competition> EnsureCompetitionAsync(Guid competitionId, CancellationToken cancellationToken)
    {
        return await DbContext.Competitions.SingleOrDefaultAsync(x => x.Id == competitionId, cancellationToken)
            ?? throw new InvalidOperationException("Competition was not found.");
    }

    private async Task CreateVersionAsync(Competition competition, Guid userId, string summary, CancellationToken cancellationToken)
    {
        var nextVersion = await DbContext.CompetitionVersions.Where(x => x.CompetitionId == competition.Id).Select(x => (int?)x.VersionNumber).MaxAsync(cancellationToken) ?? 0;
        var fields = await DbContext.CompetitionFields.Where(x => x.CompetitionId == competition.Id).OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken);
        var rules = await DbContext.CompetitionRulesVersions.Where(x => x.CompetitionId == competition.Id).OrderByDescending(x => x.VersionNumber).FirstOrDefaultAsync(cancellationToken);
        DbContext.CompetitionVersions.Add(new CompetitionVersion
        {
            Id = Guid.NewGuid(),
            CompetitionId = competition.Id,
            VersionNumber = nextVersion + 1,
            ConfigurationJson = JsonSerializer.Serialize(new { competition.Name, competition.Slug, competition.StartsAt, competition.EndsAt, competition.EntryLimit, competition.NumberOfWinners, competition.NumberOfReserveWinners, competition.MinimumAge, competition.AllowedCountryCodesJson, competition.RequiresEmailVerification, competition.RequiresPhoneVerification, Fields = fields.Select(x => new { x.FieldKey, x.FieldType, x.IsRequired }), RulesHash = rules?.ContentHash }),
            RulesVersionId = rules?.Id,
            CreatedByUserId = userId,
            ChangeSummary = summary,
            CreatedAt = DateTimeOffset.UtcNow,
            PublishedAt = DateTimeOffset.UtcNow
        });
    }

    private async Task AuditAsync(Guid userId, string action, string entityType, Guid entityId, CancellationToken cancellationToken, string? metadata = null)
    {
        await auditLogger.RecordAsync(new AuditRecord(TenantContext.TenantId, userId, "User", action, entityType, entityId.ToString(), null, Guid.NewGuid().ToString("N")), cancellationToken);
    }

    private static CompetitionPage DefaultPage(Competition competition, DateTimeOffset now)
    {
        return new CompetitionPage
        {
            Id = Guid.NewGuid(),
            CompetitionId = competition.Id,
            LanguageCode = competition.DefaultLanguage,
            Title = competition.Name,
            SeoTitle = competition.Name,
            SeoDescription = competition.Description,
            LayoutJson = JsonSerializer.Serialize(new { schemaVersion = 1, blocks = new object[] { new { id = "hero-1", type = "hero", settings = new { headline = competition.Name, subheading = competition.Description ?? "Enter the competition" } }, new { id = "entry-1", type = "EntryForm", settings = new { } } } }),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static CompetitionField BuildField(Guid competitionId, UpsertCompetitionFieldRequest request, Guid id)
    {
        var field = new CompetitionField { Id = id, CompetitionId = competitionId, CreatedAt = DateTimeOffset.UtcNow };
        ApplyField(field, request);
        return field;
    }

    private static void ApplyField(CompetitionField field, UpsertCompetitionFieldRequest request)
    {
        if (!Enum.TryParse<CompetitionFieldType>(request.FieldType, true, out var fieldType))
        {
            throw new InvalidOperationException("Field type is not valid.");
        }

        field.FieldKey = NormalizeKey(request.FieldKey);
        field.FieldType = fieldType;
        field.Label = request.Label.Trim();
        field.Placeholder = request.Placeholder;
        field.HelpText = request.HelpText;
        field.IsRequired = request.IsRequired;
        field.DisplayOrder = request.DisplayOrder;
        field.ValidationJson = string.IsNullOrWhiteSpace(request.ValidationJson) ? "{}" : request.ValidationJson;
        field.OptionsJson = string.IsNullOrWhiteSpace(request.OptionsJson) ? "{}" : request.OptionsJson;
        field.IsSensitive = request.IsSensitive;
        field.IsSearchable = request.IsSearchable;
        field.IsExportable = request.IsExportable;
        field.UpdatedAt = DateTimeOffset.UtcNow;
        _ = JsonDocument.Parse(field.ValidationJson);
        _ = JsonDocument.Parse(field.OptionsJson);
    }

    private static void ValidateCompetition(string name, string slug, DateTimeOffset startsAt, DateTimeOffset endsAt, int perParticipantLimit, int winners)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(slug))
        {
            throw new InvalidOperationException("Competition name and slug are required.");
        }

        if (endsAt <= startsAt)
        {
            throw new InvalidOperationException("Competition end date must be after the start date.");
        }

        if (perParticipantLimit < 1 || winners < 1)
        {
            throw new InvalidOperationException("Entry and winner limits must be positive.");
        }
    }

    private static string NormalizeSlug(string slug) => slug.Trim().ToLowerInvariant().Replace(' ', '-');
    private static string NormalizeKey(string value) => value.Trim().ToLowerInvariant().Replace(' ', '_');
    private static string[] NormalizeCountries(IReadOnlyList<string>? values) => values?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToUpperInvariant()).Distinct().ToArray() ?? [];
    private static IReadOnlyList<string> ParseCountries(string value) => JsonSerializer.Deserialize<string[]>(value) ?? [];
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static CompetitionResponse ToResponse(Competition competition) => new(competition.Id, competition.TenantId, competition.Name, competition.Slug, competition.Status.ToString(), competition.CompetitionType.ToString(), competition.DefaultLanguage, competition.TimeZone, competition.StartsAt, competition.EndsAt, competition.EntryLimit, competition.PerParticipantEntryLimit, competition.NumberOfWinners, competition.NumberOfReserveWinners, competition.RequiresManualApproval, competition.MinimumAge, ParseCountries(competition.AllowedCountryCodesJson), competition.RequiresEmailVerification, competition.RequiresPhoneVerification, competition.PublishedAt, competition.ClosedAt);
    private static CompetitionFieldResponse ToResponse(CompetitionField field) => new(field.Id, field.CompetitionId, field.FieldKey, field.FieldType.ToString(), field.Label, field.Placeholder, field.HelpText, field.IsRequired, field.DisplayOrder, field.ValidationJson, field.OptionsJson, field.IsSensitive, field.IsSearchable, field.IsExportable);
    private static RulesVersionResponse ToResponse(CompetitionRulesVersion rules) => new(rules.Id, rules.CompetitionId, rules.VersionNumber, rules.LanguageCode, rules.Title, rules.Content, rules.ContentHash, rules.EffectiveAt, rules.CreatedAt);
    private static CompetitionPageResponse ToResponse(CompetitionPage page) => new(page.Id, page.CompetitionId, page.LanguageCode, page.Status, page.Title, page.SeoTitle, page.SeoDescription, page.LayoutJson, page.PublishedVersion, page.PublishedAt);
}
