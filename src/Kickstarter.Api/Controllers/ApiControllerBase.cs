using System.Security.Claims;
using FluentValidation;
using Kickstarter.Application.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Kickstarter.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId)
            ? userId
            : null;

    protected async Task<ProblemHttpResult?> ValidateAsync<T>(
        T request,
        IValidator<T> validator,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);

        if (validation.IsValid)
        {
            return null;
        }

        var errors = validation
            .Errors.GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());

        return TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Request validation failed.",
            extensions: new Dictionary<string, object?> { ["errors"] = errors }
        );
    }

    protected Results<Ok<T>, ProblemHttpResult> ToOkResult<T>(ServiceResult<T> result)
    {
        return result.ResultType == ResultType.Success && result.Value is { } value
            ? TypedResults.Ok(value)
            : ToProblem(result.ResultType, result.Error);
    }

    protected Results<Created<T>, ProblemHttpResult> ToCreatedResult<T>(ServiceResult<T> result, string location)
    {
        return result.ResultType == ResultType.Created && result.Value is { } value
            ? TypedResults.Created(location, value)
            : ToProblem(result.ResultType, result.Error);
    }

    protected Results<Created<T>, ProblemHttpResult> ToCreatedResult<T>(
        ServiceResult<T> result,
        Func<T, string> location
    )
    {
        return result.ResultType == ResultType.Created && result.Value is { } value
            ? TypedResults.Created(location(value), value)
            : ToProblem(result.ResultType, result.Error);
    }

    protected Results<NoContent, ProblemHttpResult> ToNoContentResult(ServiceResult result)
    {
        return result.ResultType == ResultType.NoContent
            ? TypedResults.NoContent()
            : ToProblem(result.ResultType, result.Error);
    }

    private static ProblemHttpResult ToProblem(ResultType resultType, string? error)
    {
        return TypedResults.Problem(
            statusCode: resultType switch
            {
                ResultType.ValidationError => StatusCodes.Status400BadRequest,
                ResultType.Unauthorized => StatusCodes.Status401Unauthorized,
                ResultType.Forbidden => StatusCodes.Status403Forbidden,
                ResultType.NotFound => StatusCodes.Status404NotFound,
                ResultType.Conflict => StatusCodes.Status409Conflict,
                ResultType.Unavailable => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError,
            },
            title: error ?? "The request could not be completed."
        );
    }
}
