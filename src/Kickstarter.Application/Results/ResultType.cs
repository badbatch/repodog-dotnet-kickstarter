namespace Kickstarter.Application.Results;

public enum ResultType
{
    Success,
    Created,
    NoContent,
    ValidationError,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Unavailable,
    Failure,
}
