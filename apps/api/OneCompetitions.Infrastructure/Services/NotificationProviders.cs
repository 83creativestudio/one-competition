using System.Net;
using System.Net.Mail;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneCompetitions.Application.Notifications;

namespace OneCompetitions.Infrastructure.Services;

public sealed class SmtpEmailProvider(IConfiguration configuration) : IEmailProvider
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        using var smtp = new SmtpClient(configuration["SMTP_HOST"] ?? throw new InvalidOperationException("SMTP_HOST is required."), int.Parse(configuration["SMTP_PORT"] ?? "587"))
        {
            EnableSsl = !string.Equals(configuration["SMTP_USE_TLS"], "false", StringComparison.OrdinalIgnoreCase),
            Credentials = new NetworkCredential(configuration["SMTP_USERNAME"], configuration["SMTP_PASSWORD"])
        };
        using var mail = new MailMessage
        {
            From = new MailAddress(configuration["EMAIL_FROM_ADDRESS"] ?? throw new InvalidOperationException("EMAIL_FROM_ADDRESS is required."), configuration["EMAIL_FROM_NAME"] ?? "ONE. Competitions"),
            Subject = message.Subject,
            Body = message.BodyHtml ?? message.BodyText,
            IsBodyHtml = message.BodyHtml is not null
        };
        mail.To.Add(message.Recipient);
        await smtp.SendMailAsync(mail, cancellationToken);
    }
}

public sealed class HttpSmsProvider(HttpClient client, IConfiguration configuration) : ISmsProvider
{
    public async Task SendAsync(SmsMessage message, CancellationToken cancellationToken)
    {
        var endpoint = configuration["SMS_PROVIDER_ENDPOINT"] ?? throw new InvalidOperationException("SMS_PROVIDER_ENDPOINT is required.");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = JsonContent.Create(new { to = message.Recipient, body = message.Body }) };
        var token = configuration["SMS_PROVIDER_TOKEN"];
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public sealed class DevelopmentEmailProvider(ILogger<DevelopmentEmailProvider> logger) : IEmailProvider
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Development email queued for {Recipient} with subject {Subject}", Mask(message.Recipient), message.Subject);
        return Task.CompletedTask;
    }

    private static string Mask(string value) => value.Length < 4 ? "***" : $"{value[..2]}***{value[^2..]}";
}

public sealed class DevelopmentSmsProvider(ILogger<DevelopmentSmsProvider> logger) : ISmsProvider
{
    public Task SendAsync(SmsMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Development SMS queued for recipient ending {Suffix}", message.Recipient[^Math.Min(2, message.Recipient.Length)..]);
        return Task.CompletedTask;
    }
}
