using System.Net;
using System.Net.Mail;
using Kickstarter.Application.Users;
using Microsoft.Extensions.Options;

namespace Kickstarter.Infrastructure.Users;

public sealed class SmtpEmailSender(IOptions<SmtpEmailOptions> options) : IEmailSender
{
    public async Task SendAsync(
        string recipient,
        string subject,
        string textBody,
        CancellationToken cancellationToken = default
    )
    {
        var settings = options.Value;
        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromAddress, settings.FromName),
            Subject = subject,
            Body = textBody,
            IsBodyHtml = false,
        };
        message.To.Add(new MailAddress(recipient));

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.UseTls,
            Timeout = 15000,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        if (!string.IsNullOrWhiteSpace(settings.UserName))
        {
            client.Credentials = new NetworkCredential(settings.UserName, settings.Password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
