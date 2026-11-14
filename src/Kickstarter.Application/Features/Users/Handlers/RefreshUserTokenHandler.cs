using System.Security.Claims;
using Kickstarter.Application.Features.Users.Commands;
using Kickstarter.Application.Results;
using Kickstarter.Application.Users;
using MediatR;

namespace Kickstarter.Application.Features.Users.Handlers;

public sealed class RefreshUserTokenHandler(
    IIdentityRepository identityRepository,
    ILogger<RefreshUserTokenHandler> logger
) : IRequestHandler<RefreshUserTokenCommand, ServiceResult<ClaimsPrincipal>>
{
    public async Task<ServiceResult<ClaimsPrincipal>> Handle(
        RefreshUserTokenCommand request,
        CancellationToken cancellationToken
    )
    {
        var principal = await identityRepository.RefreshPrincipalAsync(request.RefreshToken);

        if (principal is null)
        {
            logger.LogWarning("User token refresh failed");
            return ServiceResult<ClaimsPrincipal>.Unauthorized();
        }

        logger.LogInformation("User token refresh succeeded");
        return ServiceResult<ClaimsPrincipal>.Success(principal);
    }
}
