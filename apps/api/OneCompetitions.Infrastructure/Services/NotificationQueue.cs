using OneCompetitions.Application.Notifications;
using OneCompetitions.Domain.Notifications;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class NotificationQueue(AppDbContext dbContext) : INotificationQueue
{
    public Task<Guid> QueueEmailAsync(Guid tenantId, Guid? competitionId, EmailMessage message, CancellationToken cancellationToken) =>
        QueueAsync(tenantId, competitionId, "Email", message.Recipient, message.Subject, message.BodyText, message.BodyHtml, cancellationToken);

    public Task<Guid> QueueSmsAsync(Guid tenantId, Guid? competitionId, SmsMessage message, CancellationToken cancellationToken) =>
        QueueAsync(tenantId, competitionId, "Sms", message.Recipient, null, message.Body, null, cancellationToken);

    private async Task<Guid> QueueAsync(Guid tenantId, Guid? competitionId, string channel, string recipient, string? subject, string bodyText, string? bodyHtml, CancellationToken cancellationToken)
    {
        var item = new NotificationMessage
        {
            Id = Guid.NewGuid(), TenantId = tenantId, CompetitionId = competitionId, Channel = channel,
            Recipient = recipient, Subject = subject, BodyText = bodyText, BodyHtml = bodyHtml,
            Status = "Pending", AvailableAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.NotificationMessages.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return item.Id;
    }
}
