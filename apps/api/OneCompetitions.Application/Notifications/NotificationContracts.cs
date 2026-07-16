namespace OneCompetitions.Application.Notifications;

public sealed record EmailMessage(string Recipient, string Subject, string BodyText, string? BodyHtml = null);
public sealed record SmsMessage(string Recipient, string Body);

public interface IEmailProvider
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

public interface ISmsProvider
{
    Task SendAsync(SmsMessage message, CancellationToken cancellationToken);
}

public interface INotificationQueue
{
    Task<Guid> QueueEmailAsync(Guid tenantId, Guid? competitionId, EmailMessage message, CancellationToken cancellationToken);
    Task<Guid> QueueSmsAsync(Guid tenantId, Guid? competitionId, SmsMessage message, CancellationToken cancellationToken);
}
