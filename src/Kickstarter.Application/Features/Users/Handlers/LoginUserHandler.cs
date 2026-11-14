using System.Security.Claims;
using Kickstarter.Application.Features.Users.Commands;
using Kickstarter.Application.Results;
using Kickstarter.Application.Users;
using MediatR;

namespace Kickstarter.Application.Features.Users.Handlers;

public sealed class LoginUserHandler(IIdentityRepository identityRepository, ILogger<LoginUserHandler> logger)
    : IRequestHandler<LoginUserCommand, ServiceResult<ClaimsPrincipal>>
{
    public async Task<ServiceResult<ClaimsPrincipal>> Handle(
        LoginUserCommand request,
        CancellationToken cancellationToken
    )
    {
        var principal = await identityRepository.SignInAsync(request.Email.Trim(), request.Password);

        if (principal is null)
        {
            logger.LogWarning("User authentication failed");
            return ServiceResult<ClaimsPrincipal>.Unauthorized();
        }

        logger.LogInformation("User authentication succeeded");
        return ServiceResult<ClaimsPrincipal>.Success(principal);
    }
}
