using Asp.Versioning;
using Kickstarter.Application.Features.Health.Queries;
using Kickstarter.Application.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Kickstarter.Api.Controllers;

[ApiController]
[ApiVersionNeutral]
[Route("health")]
public sealed class HealthController(IMediator mediator) : ApiControllerBase
{
    [HttpGet(Name = "getHealth")]
    public Results<Ok<object>, ProblemHttpResult> Get()
    {
        return TypedResults.Ok<object>(new { status = "healthy" });
    }

    [HttpGet("database", Name = "getDatabaseHealth")]
    public async Task<Results<Ok<object>, ProblemHttpResult>> GetDatabaseAsync(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CheckDatabaseHealthQuery(), cancellationToken);

        return result.ResultType == ResultType.Success
            ? TypedResults.Ok<object>(new { status = "healthy", database = result.Value })
            : TypedResults.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: result.Error);
    }
}
