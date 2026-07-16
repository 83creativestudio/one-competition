using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneCompetitions.Application.Auditing;
using OneCompetitions.Application.Consents;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Contracts.Consents;
using OneCompetitions.Domain.Consents;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class ConsentService(AppDbContext dbContext, ITenantContext tenantContext, UserManager<ApplicationUser> userManager, IAuditLogger auditLogger)
    : TenantScopedServiceBase(dbContext, tenantContext, userManager), IConsentService
{
    public async Task<IReadOnlyList<ConsentDefinitionResponse>> ListAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        return await DbContext.ConsentDefinitions.Where(x => x.CompetitionId == competitionId).OrderBy(x => x.ConsentType).ThenByDescending(x => x.Version).Select(x => ToResponse(x)).ToListAsync(cancellationToken);
    }

    public async Task<ConsentDefinitionResponse> CreateVersionAsync(Guid userId, Guid competitionId, CreateConsentDefinitionRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        if (!Enum.TryParse<ConsentType>(request.ConsentType, true, out var type)) throw new InvalidOperationException("Consent type is invalid.");
        if (type.ToString().StartsWith("Marketing", StringComparison.Ordinal) && request.IsRequired)
            throw new InvalidOperationException("Marketing consent cannot be required for competition entry.");
        if (string.IsNullOrWhiteSpace(request.Text)) throw new InvalidOperationException("Consent text is required.");
        var language = request.LanguageCode.Trim().ToLowerInvariant();
        var version = await DbContext.ConsentDefinitions.Where(x => x.CompetitionId == competitionId && x.ConsentType == type && x.LanguageCode == language).MaxAsync(x => (int?)x.Version, cancellationToken) ?? 0;
        var entity = new ConsentDefinition { Id = Guid.NewGuid(), TenantId = TenantContext.TenantId, CompetitionId = competitionId, ConsentType = type,
            LanguageCode = language, Text = request.Text.Trim(), IsRequired = request.IsRequired, Version = version + 1, CreatedAt = DateTimeOffset.UtcNow };
        DbContext.ConsentDefinitions.Add(entity); await DbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(TenantContext.TenantId, userId, "User", "consent.version_created", "ConsentDefinition", entity.Id.ToString(), null, Guid.NewGuid().ToString("N")), cancellationToken);
        return ToResponse(entity);
    }

    private static ConsentDefinitionResponse ToResponse(ConsentDefinition x) => new(x.Id, x.CompetitionId!.Value, x.ConsentType.ToString(), x.LanguageCode, x.Text, x.IsRequired, x.Version, x.CreatedAt);
}
