namespace Kickstarter.Application.Users;

public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string textBody, CancellationToken cancellationToken = default);
}
