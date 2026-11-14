using Kickstarter.Application.Features.Users.Commands;
using Kickstarter.Application.Results;
using Kickstarter.Application.Users;
using MediatR;
using Microsoft.Extensions.Options;

namespace Kickstarter.Application.Features.Users.Handlers;

public sealed class ForgotPasswordHandler(
    IIdentityRepository identityRepository,
    IEmailSender emailSender,
    IOptions<AccountRecoveryOptions> options,
    ILogger<ForgotPasswordHandler> logger
) : IRequestHandler<ForgotPasswordCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var reset = await identityRepository.GeneratePasswordResetTokenAsync(request.Email.Trim(), cancellationToken);

        if (reset is not null)
        {
            var resetUrl = BuildResetUrl(options.Value.PasswordResetUrl, reset.Value.Email, reset.Value.Token);

            try
            {
                await emailSender.SendAsync(
                    reset.Value.Email,
                    "Reset your Kickstarter password",
                    $"Use this link to reset your password. If you did not request a reset, you can ignore this email.\n\n{resetUrl}",
                    cancellationToken
                );
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Could not send a password reset message");
            }
        }

        return ServiceResult.NoContent();
    }

    private static string BuildResetUrl(string configuredUrl, string email, string token)
    {
        var builder = new UriBuilder(configuredUrl);
        var query = new Dictionary<string, string> { ["email"] = email, ["token"] = token };
        builder.Query = string.Join(
            "&",
            query.Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}")
        );
        return builder.Uri.ToString();
    }
}
