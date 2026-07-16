using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneCompetitions.Application.Domains;
using OneCompetitions.Application.Draws;
using OneCompetitions.Application.Jobs;
using OneCompetitions.Application.Locking;
using OneCompetitions.Application.Notifications;
using OneCompetitions.Application.Storage;
using OneCompetitions.Domain.Auditing;
using OneCompetitions.Domain.Competitions;
using OneCompetitions.Domain.Notifications;
using OneCompetitions.Domain.Tenants;
using OneCompetitions.Domain.Winners;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class PlatformJobProcessor(
    AppDbContext dbContext,
    IDistributedLockProvider locks,
    IDomainVerificationProvider domainVerification,
    ISslProvisioningProvider sslProvisioning,
    IEmailProvider emailProvider,
    ISmsProvider smsProvider,
    IObjectStorage storage,
    IDrawCertificateService certificates,
    SecretProtector protector,
    IHttpClientFactory httpClientFactory,
    ILogger<PlatformJobProcessor> logger) : IPlatformJobProcessor
{
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(5);

    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var handle = await locks.TryAcquireAsync("jobs:platform-cycle", TimeSpan.FromMinutes(2), cancellationToken);
        if (handle is null) return 0;
        var processed = 0;
        processed += await CloseCompetitionsAsync(cancellationToken);
        processed += await CheckDomainsAsync(cancellationToken);
        processed += await ProcessNotificationsAsync(cancellationToken);
        processed += await ProcessExportsAsync(cancellationToken);
        processed += await ProcessWebhooksAsync(cancellationToken);
        processed += await certificates.GeneratePendingAsync(cancellationToken);
        processed += await QueueWinnerRemindersAsync(cancellationToken);
        processed += await ApplyRetentionAsync(cancellationToken);
        return processed;
    }

    private async Task<int> CloseCompetitionsAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var closableStatuses = new[] { CompetitionStatus.Live, CompetitionStatus.Paused, CompetitionStatus.Scheduled };
        var competitions = (await dbContext.Competitions.IgnoreQueryFilters()
            .Where(x => closableStatuses.Contains(x.Status)).Take(200).ToListAsync(cancellationToken))
            .Where(x => x.EndsAt <= now).Take(50).ToList();
        foreach (var competition in competitions)
        {
            competition.Status = CompetitionStatus.Closed;
            competition.ClosedAt = now;
            competition.UpdatedAt = now;
            dbContext.AuditEvents.Add(SystemAudit(competition.TenantId, "competition.closed.automatic", "Competition", competition.Id, competition.Id));
        }
        if (competitions.Count > 0) await dbContext.SaveChangesAsync(cancellationToken);
        return competitions.Count;
    }

    private async Task<int> CheckDomainsAsync(CancellationToken cancellationToken)
    {
        var due = DateTimeOffset.UtcNow.AddMinutes(-15);
        var domains = (await dbContext.TenantDomains.IgnoreQueryFilters()
            .Where(x => x.DeletedAt == null && x.DomainType != TenantDomainType.PlatformPath && x.DomainType != TenantDomainType.PlatformSubdomain
                && (x.Status == TenantDomainStatus.AwaitingDns || x.Status == TenantDomainStatus.DnsError || x.Status == TenantDomainStatus.Verifying
                    || x.Status == TenantDomainStatus.ProvisioningSsl || x.Status == TenantDomainStatus.SslError
                    || x.Status == TenantDomainStatus.Active)).Take(100).ToListAsync(cancellationToken))
            .Where(x => (x.LastCheckedAt == null || x.LastCheckedAt < due)
                && (x.Status != TenantDomainStatus.Active || x.CertificateExpiresAt < DateTimeOffset.UtcNow.AddDays(30)))
            .Take(20).ToList();
        foreach (var domain in domains)
        {
            try
            {
                domain.LastCheckedAt = DateTimeOffset.UtcNow;
                if (domain.VerifiedAt is null)
                {
                    var verification = await domainVerification.VerifyAsync(domain.Hostname, domain.VerificationToken ?? string.Empty, cancellationToken);
                    if (!verification.IsVerified)
                    {
                        domain.Status = TenantDomainStatus.DnsError;
                        domain.FailureReason = verification.FailureReason;
                        continue;
                    }
                    domain.VerifiedAt = DateTimeOffset.UtcNow;
                }
                domain.Status = TenantDomainStatus.ProvisioningSsl;
                var ssl = await sslProvisioning.ProvisionAsync(domain.Hostname, cancellationToken);
                domain.SslStatus = ssl.IsProvisioned ? "Active" : "Pending";
                domain.CertificateExpiresAt = ssl.CertificateExpiresAt;
                domain.FailureReason = ssl.FailureReason;
                domain.Status = ssl.IsProvisioned ? TenantDomainStatus.Active : TenantDomainStatus.ProvisioningSsl;
                if (ssl.IsProvisioned) domain.SslProvisionedAt ??= DateTimeOffset.UtcNow;
            }
            catch (Exception exception)
            {
                domain.Status = domain.VerifiedAt is null ? TenantDomainStatus.DnsError : TenantDomainStatus.SslError;
                domain.FailureReason = SafeMessage(exception);
                logger.LogWarning(exception, "Domain check failed for domain {DomainId}", domain.Id);
            }
            finally
            {
                domain.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
        if (domains.Count > 0) await dbContext.SaveChangesAsync(cancellationToken);
        return domains.Count;
    }

    private async Task<int> ProcessNotificationsAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var messages = (await dbContext.NotificationMessages.IgnoreQueryFilters()
            .Where(x => x.Status == "Pending" || x.Status == "Retry").Take(200).ToListAsync(cancellationToken))
            .Where(x => x.AvailableAt <= now && (x.LockedUntil == null || x.LockedUntil < now)).OrderBy(x => x.CreatedAt).Take(25).ToList();
        foreach (var message in messages) { message.Status = "Processing"; message.LockedUntil = now.Add(Lease); }
        if (messages.Count > 0) await dbContext.SaveChangesAsync(cancellationToken);
        foreach (var message in messages)
        {
            try
            {
                if (message.Channel == "Email") await emailProvider.SendAsync(new EmailMessage(message.Recipient, message.Subject ?? string.Empty, message.BodyText, message.BodyHtml), cancellationToken);
                else if (message.Channel == "Sms") await smsProvider.SendAsync(new SmsMessage(message.Recipient, message.BodyText), cancellationToken);
                else throw new InvalidOperationException("Notification channel is unsupported.");
                message.Status = "Sent";
                message.SentAt = DateTimeOffset.UtcNow;
                message.LastError = null;
            }
            catch (Exception exception)
            {
                Retry(message, exception);
                logger.LogWarning(exception, "Notification delivery failed for message {MessageId}", message.Id);
            }
            message.LockedUntil = null;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return messages.Count;
    }

    private async Task<int> ProcessExportsAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var jobs = (await dbContext.ExportJobs.IgnoreQueryFilters().Where(x => x.Status == "Pending" || x.Status == "Retry")
            .Take(50).ToListAsync(cancellationToken))
            .Where(x => x.LockedUntil == null || x.LockedUntil < now).OrderBy(x => x.CreatedAt).Take(5).ToList();
        foreach (var job in jobs) { job.Status = "Processing"; job.LockedUntil = now.Add(Lease); }
        if (jobs.Count > 0) await dbContext.SaveChangesAsync(cancellationToken);
        foreach (var job in jobs)
        {
            try
            {
                if (!string.Equals(job.Format, "Csv", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Only CSV exports are currently supported.");
                var csv = await BuildCsvAsync(job.TenantId, job.CompetitionId, job.ExportType, cancellationToken);
                job.StorageKey = $"tenant/{job.TenantId:N}/exports/{job.Id:N}.csv";
                await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
                await storage.PutAsync(job.StorageKey, stream, "text/csv", cancellationToken);
                job.Status = "Completed";
                job.CompletedAt = DateTimeOffset.UtcNow;
                job.FailureReason = null;
            }
            catch (Exception exception)
            {
                job.AttemptCount++;
                job.Status = job.AttemptCount >= 5 ? "Failed" : "Retry";
                job.FailureReason = SafeMessage(exception);
            }
            job.LockedUntil = null;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return jobs.Count;
    }

    private async Task<string> BuildCsvAsync(Guid tenantId, Guid? competitionId, string exportType, CancellationToken cancellationToken)
    {
        var output = new StringBuilder();
        if (exportType.Equals("Entries", StringComparison.OrdinalIgnoreCase))
        {
            output.AppendLine("EntryReference,Status,EligibilityStatus,RiskScore,RiskLevel,SubmittedAt,Email,Phone");
            var rows = await (from entry in dbContext.CompetitionEntries.IgnoreQueryFilters()
                              join participant in dbContext.Participants.IgnoreQueryFilters() on entry.ParticipantId equals participant.Id
                              where entry.TenantId == tenantId && entry.CompetitionId == competitionId
                              select new { entry.EntryReference, Status = entry.Status.ToString(), Eligibility = entry.EligibilityStatus.ToString(), entry.RiskScore, Risk = entry.RiskLevel.ToString(), entry.SubmittedAt, participant.PrimaryEmail, participant.PrimaryPhone }).ToListAsync(cancellationToken);
            foreach (var row in rows) output.AppendLine(string.Join(',', Csv(row.EntryReference), Csv(row.Status), Csv(row.Eligibility), row.RiskScore, Csv(row.Risk), Csv(row.SubmittedAt?.ToString("O")), Csv(row.PrimaryEmail), Csv(row.PrimaryPhone)));
        }
        else if (exportType.Equals("Participants", StringComparison.OrdinalIgnoreCase))
        {
            output.AppendLine("Email,Phone,FirstName,LastName,CountryCode,City,CreatedAt");
            var rows = await (from participant in dbContext.Participants.IgnoreQueryFilters()
                              join entry in dbContext.CompetitionEntries.IgnoreQueryFilters() on participant.Id equals entry.ParticipantId
                              where participant.TenantId == tenantId && entry.CompetitionId == competitionId
                              select participant).Distinct().ToListAsync(cancellationToken);
            foreach (var row in rows) output.AppendLine(string.Join(',', Csv(row.PrimaryEmail), Csv(row.PrimaryPhone), Csv(row.FirstName), Csv(row.LastName), Csv(row.CountryCode), Csv(row.City), Csv(row.CreatedAt.ToString("O"))));
        }
        else throw new InvalidOperationException("Export type is unsupported.");
        return output.ToString();
    }

    private async Task<int> ProcessWebhooksAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var deliveries = (await dbContext.WebhookDeliveries.Where(x => x.Status == "Pending" || x.Status == "Retry")
            .Take(100).ToListAsync(cancellationToken))
            .Where(x => x.NextAttemptAt <= now && (x.LockedUntil == null || x.LockedUntil < now)).OrderBy(x => x.CreatedAt).Take(20).ToList();
        foreach (var delivery in deliveries) { delivery.Status = "Processing"; delivery.LockedUntil = now.Add(Lease); }
        if (deliveries.Count > 0) await dbContext.SaveChangesAsync(cancellationToken);
        foreach (var delivery in deliveries)
        {
            var endpoint = await dbContext.WebhookEndpoints.IgnoreQueryFilters().SingleAsync(x => x.Id == delivery.WebhookEndpointId, cancellationToken);
            var webhookEvent = await dbContext.WebhookEvents.IgnoreQueryFilters().SingleAsync(x => x.Id == delivery.WebhookEventId, cancellationToken);
            try
            {
                if (!endpoint.IsActive) throw new InvalidOperationException("Webhook endpoint is disabled.");
                await WebhookUrlValidator.EnsurePublicHttpsAsync(new Uri(endpoint.Url), cancellationToken);
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                var payload = JsonSerializer.Serialize(new { id = webhookEvent.Id, type = webhookEvent.EventType, createdAt = webhookEvent.CreatedAt, data = JsonSerializer.Deserialize<JsonElement>(webhookEvent.PayloadJson) });
                var secret = protector.Unprotect(endpoint.SecretCiphertext);
                var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{payload}"))).ToLowerInvariant();
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url) { Content = new StringContent(payload, Encoding.UTF8, "application/json") };
                request.Headers.Add("X-One-Delivery", delivery.Id.ToString());
                request.Headers.Add("X-One-Timestamp", timestamp);
                request.Headers.Add("X-One-Signature", $"v1={signature}");
                using var response = await httpClientFactory.CreateClient("webhooks").SendAsync(request, cancellationToken);
                delivery.LastStatusCode = (int)response.StatusCode;
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                delivery.ResponseBodyPreview = responseBody[..Math.Min(responseBody.Length, 1000)];
                if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Webhook returned {(int)response.StatusCode}.");
                delivery.Status = "Delivered";
                delivery.DeliveredAt = DateTimeOffset.UtcNow;
                endpoint.ConsecutiveFailureCount = 0;
            }
            catch (Exception exception)
            {
                delivery.AttemptCount++;
                delivery.LastError = SafeMessage(exception);
                delivery.Status = delivery.AttemptCount >= 8 ? "DeadLetter" : "Retry";
                delivery.NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(Math.Pow(2, Math.Min(delivery.AttemptCount, 6)));
                endpoint.ConsecutiveFailureCount++;
                if (endpoint.ConsecutiveFailureCount >= 20) endpoint.IsActive = false;
                logger.LogWarning(exception, "Webhook delivery {DeliveryId} failed", delivery.Id);
            }
            endpoint.UpdatedAt = DateTimeOffset.UtcNow;
            delivery.LockedUntil = null;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return deliveries.Count;
    }

    private async Task<int> QueueWinnerRemindersAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var candidates = (await (from claim in dbContext.WinnerClaims
                                join result in dbContext.DrawResults on claim.DrawResultId equals result.Id
                                join draw in dbContext.Draws.IgnoreQueryFilters() on result.DrawId equals draw.Id
                                join entry in dbContext.CompetitionEntries.IgnoreQueryFilters() on claim.EntryId equals entry.Id
                                join participant in dbContext.Participants.IgnoreQueryFilters() on entry.ParticipantId equals participant.Id
                                where claim.ClaimDeadline != null && (claim.Status == WinnerClaimStatus.Contacted || claim.Status == WinnerClaimStatus.Selected) && participant.PrimaryEmail != null
                                select new { claim, draw.TenantId, draw.CompetitionId, participant.PrimaryEmail, entry.EntryReference }).Take(100).ToListAsync(cancellationToken))
            .Where(x => x.claim.ClaimDeadline > now && x.claim.ClaimDeadline <= now.AddDays(2)).Take(25).ToList();
        var queued = 0;
        foreach (var candidate in candidates)
        {
            var dedup = $"winner-reminder:{candidate.claim.Id}:{now:yyyyMMdd}";
            if (await dbContext.NotificationMessages.IgnoreQueryFilters().AnyAsync(x => x.TenantId == candidate.TenantId && x.DeduplicationKey == dedup, cancellationToken)) continue;
            dbContext.NotificationMessages.Add(new NotificationMessage
            {
                Id = Guid.NewGuid(), TenantId = candidate.TenantId, CompetitionId = candidate.CompetitionId, Channel = "Email",
                Recipient = candidate.PrimaryEmail!, Subject = "Reminder: claim your competition prize",
                BodyText = $"Your winning entry {candidate.EntryReference} is awaiting your response before {candidate.claim.ClaimDeadline:u}.",
                Status = "Pending", AvailableAt = now, CreatedAt = now, DeduplicationKey = dedup
            });
            queued++;
        }
        if (queued > 0) await dbContext.SaveChangesAsync(cancellationToken);
        return queued;
    }

    private async Task<int> ApplyRetentionAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var processed = 0;
        var expiredExports = (await dbContext.ExportJobs.IgnoreQueryFilters().Where(x => x.Status == "Completed").Take(100).ToListAsync(cancellationToken))
            .Where(x => x.ExpiresAt <= now).Take(25).ToList();
        foreach (var job in expiredExports)
        {
            if (job.StorageKey is not null) await storage.DeleteAsync(job.StorageKey, cancellationToken);
            job.Status = "Expired";
            job.StorageKey = null;
            processed++;
        }
        var policies = await dbContext.DataRetentionPolicies.IgnoreQueryFilters().ToListAsync(cancellationToken);
        foreach (var policy in policies)
        {
            var cutoff = now.AddDays(-policy.AnonymiseAfterDays);
            var participantQuery = dbContext.Participants.IgnoreQueryFilters().Where(x => x.TenantId == policy.TenantId && x.AnonymisedAt == null);
            if (policy.CompetitionId is not null)
            {
                var participantIds = dbContext.CompetitionEntries.IgnoreQueryFilters().Where(x => x.CompetitionId == policy.CompetitionId).Select(x => x.ParticipantId);
                participantQuery = participantQuery.Where(x => participantIds.Contains(x.Id));
            }
            var participants = (await participantQuery.Take(100).ToListAsync(cancellationToken))
                .Where(x => x.CreatedAt < cutoff).Take(25).ToList();
            foreach (var participant in participants)
            {
                participant.PrimaryEmail = null; participant.PrimaryPhone = null; participant.FirstName = null; participant.LastName = null;
                participant.DateOfBirth = null; participant.City = null; participant.AnonymisedAt = now; participant.UpdatedAt = now;
                dbContext.AuditEvents.Add(SystemAudit(policy.TenantId, "participant.anonymised.retention", "Participant", participant.Id, null));
                processed++;
            }

            var uploadCutoff = now.AddDays(-policy.DeleteUploadsAfterDays);
            var assets = (await dbContext.Assets.IgnoreQueryFilters().Where(x => x.TenantId == policy.TenantId && x.DeletedAt == null
                    && (policy.CompetitionId == null || x.CompetitionId == policy.CompetitionId)).Take(100).ToListAsync(cancellationToken))
                .Where(x => x.CreatedAt < uploadCutoff).Take(25).ToList();
            foreach (var asset in assets)
            {
                await storage.DeleteAsync(asset.StorageKey, cancellationToken);
                asset.DeletedAt = now;
                dbContext.AuditEvents.Add(SystemAudit(policy.TenantId, "asset.deleted.retention", "Asset", asset.Id, asset.CompetitionId));
                processed++;
            }
        }
        if (processed > 0) await dbContext.SaveChangesAsync(cancellationToken);
        return processed;
    }

    private static void Retry(NotificationMessage message, Exception exception)
    {
        message.AttemptCount++;
        message.LastError = SafeMessage(exception);
        message.Status = message.AttemptCount >= 8 ? "DeadLetter" : "Retry";
        message.AvailableAt = DateTimeOffset.UtcNow.AddMinutes(Math.Pow(2, Math.Min(message.AttemptCount, 6)));
    }

    private static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    private static string SafeMessage(Exception exception) => exception.Message[..Math.Min(exception.Message.Length, 1000)];
    private static AuditEvent SystemAudit(Guid tenantId, string action, string entityType, Guid entityId, Guid? competitionId) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenantId, ActorType = "System", Action = action, EntityType = entityType,
        EntityId = entityId.ToString(), CompetitionId = competitionId, OccurredAt = DateTimeOffset.UtcNow, CorrelationId = Guid.NewGuid().ToString("N")
    };
}
