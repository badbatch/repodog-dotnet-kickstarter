using Kickstarter.Application.Contracts.Users;
using Kickstarter.Application.Features.Users.Queries;
using Kickstarter.Application.Mapping;
using Kickstarter.Application.Results;
using Kickstarter.Application.Users;
using MediatR;

namespace Kickstarter.Application.Features.Users.Handlers;

public sealed class GetCurrentUserHandler(IIdentityRepository identityRepository)
    : IRequestHandler<GetCurrentUserQuery, ServiceResult<UserResponse>>
{
    public async Task<ServiceResult<UserResponse>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken
    )
    {
        var user = await identityRepository.GetUserAsync(request.Principal);

        return user is null
            ? ServiceResult<UserResponse>.Unauthorized()
            : ServiceResult<UserResponse>.Success(user.ToResponse());
    }
}
