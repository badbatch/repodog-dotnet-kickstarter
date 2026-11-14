using Kickstarter.Application.Contracts.Users;
using Kickstarter.Application.Features.Users.Commands;
using Kickstarter.Application.Mapping;
using Kickstarter.Application.Results;
using Kickstarter.Application.Users;
using Kickstarter.Domain.Entities;
using MediatR;

namespace Kickstarter.Application.Features.Users.Handlers;

public sealed class RegisterUserHandler(IIdentityRepository identityRepository, ILogger<RegisterUserHandler> logger)
    : IRequestHandler<RegisterUserCommand, ServiceResult<UserResponse>>
{
    public async Task<ServiceResult<UserResponse>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken
    )
    {
        var email = request.Email.Trim();

        var user = new ApplicationUser
        {
            UserName = string.IsNullOrWhiteSpace(request.UserName) ? email : request.UserName.Trim(),
            Email = email,
        };

        var errors = await identityRepository.CreateUserAsync(user, request.Password);

        if (errors.Count == 0)
        {
            logger.LogInformation("Registered a new application user");
        }
        else
        {
            logger.LogInformation("User registration was rejected by identity policy");
        }

        return errors.Count == 0
            ? ServiceResult<UserResponse>.Created(user.ToResponse())
            : ServiceResult<UserResponse>.ValidationError(string.Join(" ", errors));
    }
}
