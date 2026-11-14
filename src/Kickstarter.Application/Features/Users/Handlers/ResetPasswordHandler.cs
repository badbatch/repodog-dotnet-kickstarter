using Kickstarter.Application.Features.Users.Commands;
using Kickstarter.Application.Results;
using Kickstarter.Application.Users;
using MediatR;

namespace Kickstarter.Application.Features.Users.Handlers;

public sealed class ResetPasswordHandler(IIdentityRepository identityRepository, ILogger<ResetPasswordHandler> logger)
    : IRequestHandler<ResetPasswordCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var errors = await identityRepository.ResetPasswordAsync(
            request.Email.Trim(),
            request.Token,
            request.NewPassword,
            cancellationToken
        );

        if (errors.Count > 0)
        {
            logger.LogInformation("Password reset was rejected by identity policy");
            return ServiceResult.ValidationError(
                "The password reset request is invalid or the new password is not accepted."
            );
        }

        logger.LogInformation("Password reset completed");
        return ServiceResult.NoContent();
    }
}
