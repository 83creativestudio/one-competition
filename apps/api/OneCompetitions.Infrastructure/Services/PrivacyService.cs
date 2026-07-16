using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OneCompetitions.Application.Auditing;
using OneCompetitions.Application.Notifications;
using OneCompetitions.Application.Privacy;
using OneCompetitions.Application.Storage;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Contracts.Privacy;
using OneCompetitions.Domain.Consents;
using OneCompetitions.Domain.Participants;
using OneCompetitions.Domain.Privacy;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class PrivacyService(AppDbContext dbContext, ITenantContext tenantContext, INotificationQueue notifications,
    IObjectStorage storage, SecretProtector protector, IAuditLogger auditLogger) : IPrivacyService
{
    private static readonly string[] Supported = ["Export", "Delete", "WithdrawMarketing"];

    public async Task RequestAsync(CreatePrivacyRequest request, CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved) throw new UnauthorizedAccessException("Tenant context is required.");
        if (!Supported.Contains(request.RequestType, StringComparer.OrdinalIgnoreCase)) throw new InvalidOperationException("Privacy request type is invalid.");
        var email = request.Email.Trim().ToLowerInvariant();
        var participant = await dbContext.Participants.SingleOrDefaultAsync(x => x.PrimaryEmail == email && x.DeletedAt == null, cancellationToken);
        if (participant is null) return;

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var entity = new PrivacyRequest
        {
            Id = Guid.NewGuid(), TenantId = tenantContext.TenantId, ParticipantId = participant.Id,
            Reference = $"PRV-{DateTimeOffset.UtcNow:yyyy}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}",
            RequestType = Supported.Single(x => x.Equals(request.RequestType, StringComparison.OrdinalIgnoreCase)),
            TokenHash = AuthService.Hash(token), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30), CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.PrivacyRequests.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await notifications.QueueEmailAsync(tenantContext.TenantId, null,
            new EmailMessage(email, "Confirm your privacy request", $"Privacy request: {entity.Reference}\nOne-time token: {token}\nThis token expires in 30 minutes."), cancellationToken);
    }

    public async Task<PrivacyRequestResult> CompleteAsync(string reference, CompletePrivacyRequest request, CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved) throw new UnauthorizedAccessException("Tenant context is required.");
        var entity = await dbContext.PrivacyRequests.SingleOrDefaultAsync(x => x.Reference == reference, cancellationToken)
            ?? throw new InvalidOperationException("Privacy request is invalid or expired.");
        if (entity.Status == "Completed") return await ToResultAsync(entity, cancellationToken);
        if (entity.ExpiresAt <= DateTimeOffset.UtcNow || !CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(entity.TokenHash), Encoding.UTF8.GetBytes(AuthService.Hash(request.Token))))
            throw new InvalidOperationException("Privacy request is invalid or expired.");
        var participant = await dbContext.Participants.SingleAsync(x => x.Id == entity.ParticipantId, cancellationToken);

        if (entity.RequestType == "Export") await ExportAsync(entity, participant, cancellationToken);
        else if (entity.RequestType == "Delete") await AnonymiseAsync(entity, participant, cancellationToken);
        else await WithdrawMarketingAsync(participant.Id, cancellationToken);

        entity.Status = "Completed";
        entity.CompletedAt = DateTimeOffset.UtcNow;
        entity.TokenHash = string.Empty;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.RecordAsync(new AuditRecord(tenantContext.TenantId, null, "Participant", $"privacy.{entity.RequestType.ToLowerInvariant()}", "PrivacyRequest", entity.Id.ToString(), null, Guid.NewGuid().ToString("N")), cancellationToken);
        return await ToResultAsync(entity, cancellationToken);
    }

    private async Task ExportAsync(PrivacyRequest request, Participant participant, CancellationToken cancellationToken)
    {
        var entries = await dbContext.CompetitionEntries.Where(x => x.ParticipantId == participant.Id).ToListAsync(cancellationToken);
        var entryIds = entries.Select(x => x.Id).ToArray();
        var answers = await dbContext.EntryAnswers.Where(x => entryIds.Contains(x.EntryId)).ToListAsync(cancellationToken);
        var consents = await dbContext.ParticipantConsents.Where(x => x.ParticipantId == participant.Id).ToListAsync(cancellationToken);
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            generatedAt = DateTimeOffset.UtcNow,
            participant = new { participant.PrimaryEmail, participant.PrimaryPhone, participant.FirstName, participant.LastName, participant.DateOfBirth, participant.CountryCode, participant.City, participant.PreferredLanguage },
            entries = entries.Select(x => new { x.EntryReference, status = x.Status.ToString(), x.SubmittedAt, x.VerifiedAt }),
            answers = answers.Select(x => new { x.EntryId, x.CompetitionFieldId, Value = x.EncryptedValue is null ? x.StringValue : protector.Unprotect(x.EncryptedValue), x.NumberValue, x.DateValue, x.BooleanValue, x.JsonValue }),
            consents = consents.Select(x => new { x.EntryId, x.ConsentDefinitionId, x.Accepted, x.AcceptedAt, x.WithdrawnAt })
        });
        request.ResultStorageKey = $"tenants/{tenantContext.TenantId}/privacy/{request.Reference}.json";
        await using var stream = new MemoryStream(payload);
        await storage.PutAsync(request.ResultStorageKey, stream, "application/json", cancellationToken);
    }

    private async Task AnonymiseAsync(PrivacyRequest request, Participant participant, CancellationToken cancellationToken)
    {
        await WithdrawMarketingAsync(participant.Id, cancellationToken);
        var identities = await dbContext.ParticipantIdentities.Where(x => x.ParticipantId == participant.Id).ToListAsync(cancellationToken);
        foreach (var identity in identities) identity.ProviderEmail = null;
        var entryIds = await dbContext.CompetitionEntries.Where(x => x.ParticipantId == participant.Id).Select(x => x.Id).ToListAsync(cancellationToken);
        var assets = await dbContext.Assets.Where(x => x.EntryId != null && entryIds.Contains(x.EntryId.Value) && x.DeletedAt == null).ToListAsync(cancellationToken);
        foreach (var asset in assets) { await storage.DeleteAsync(asset.StorageKey, cancellationToken); asset.DeletedAt = DateTimeOffset.UtcNow; }
        participant.PrimaryEmail = null; participant.PrimaryPhone = null; participant.FirstName = null; participant.LastName = null;
        participant.DateOfBirth = null; participant.CountryCode = null; participant.City = null; participant.Status = ParticipantStatus.Anonymised;
        participant.AnonymisedAt = DateTimeOffset.UtcNow; participant.DeletedAt = DateTimeOffset.UtcNow; participant.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private async Task WithdrawMarketingAsync(Guid participantId, CancellationToken cancellationToken)
    {
        var marketingTypes = new[] { ConsentType.MarketingEmail, ConsentType.MarketingSms, ConsentType.MarketingPhone, ConsentType.MarketingProfiling };
        var consents = await dbContext.ParticipantConsents.Where(x => x.ParticipantId == participantId && x.WithdrawnAt == null)
            .Join(dbContext.ConsentDefinitions, x => x.ConsentDefinitionId, x => x.Id, (consent, definition) => new { consent, definition.ConsentType })
            .Where(x => marketingTypes.Contains(x.ConsentType)).Select(x => x.consent).ToListAsync(cancellationToken);
        foreach (var consent in consents) consent.WithdrawnAt = DateTimeOffset.UtcNow;
    }

    private async Task<PrivacyRequestResult> ToResultAsync(PrivacyRequest request, CancellationToken cancellationToken) =>
        new(request.Reference, request.RequestType, request.Status,
            request.ResultStorageKey is null ? null : await storage.CreateDownloadUrlAsync(request.ResultStorageKey, TimeSpan.FromMinutes(10), cancellationToken), request.CompletedAt);
}
