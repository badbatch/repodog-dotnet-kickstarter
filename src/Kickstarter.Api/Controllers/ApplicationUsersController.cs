using Asp.Versioning;
using Kickstarter.Api.Authorization;
using Kickstarter.Application.Contracts.Users;
using Kickstarter.Application.Features.Users.Queries;
using Kickstarter.Application.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Kickstarter.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
public sealed class ApplicationUsersController(IMediator mediator, CurrentUserResponseFactory responseFactory)
    : ApiControllerBase
{
    [HttpGet("me", Name = "getCurrentUser")]
    public async Task<Results<Ok<CurrentUserResponse>, ProblemHttpResult>> GetMeAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetCurrentUserQuery(User), ct);

        if (result.ResultType != ResultType.Success || result.Value is not { } user)
        {
            return ToOkResult(new ServiceResult<CurrentUserResponse>(result.ResultType, Error: result.Error));
        }

        return TypedResults.Ok(await responseFactory.CreateAsync(user, User));
    }
}
