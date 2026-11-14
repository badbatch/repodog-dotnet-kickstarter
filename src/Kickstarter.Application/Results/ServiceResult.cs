namespace Kickstarter.Application.Results;

public sealed record ServiceResult<T>(ResultType ResultType, T? Value = default, string? Error = null)
{
    public bool Succeeded => ResultType is ResultType.Success or ResultType.Created or ResultType.NoContent;

    public static ServiceResult<T> Success(T value)
    {
        return new ServiceResult<T>(ResultType.Success, value);
    }

    public static ServiceResult<T> Created(T value)
    {
        return new ServiceResult<T>(ResultType.Created, value);
    }

    public static ServiceResult<T> NoContent()
    {
        return new ServiceResult<T>(ResultType.NoContent);
    }

    public static ServiceResult<T> ValidationError(string error)
    {
        return new ServiceResult<T>(ResultType.ValidationError, Error: error);
    }

    public static ServiceResult<T> Unauthorized(string? error = null)
    {
        return new ServiceResult<T>(ResultType.Unauthorized, Error: error);
    }

    public static ServiceResult<T> Forbidden(string? error = null)
    {
        return new ServiceResult<T>(ResultType.Forbidden, Error: error);
    }

    public static ServiceResult<T> NotFound(string? error = null)
    {
        return new ServiceResult<T>(ResultType.NotFound, Error: error);
    }

    public static ServiceResult<T> Conflict(string error)
    {
        return new ServiceResult<T>(ResultType.Conflict, Error: error);
    }

    public static ServiceResult<T> Unavailable(string error)
    {
        return new ServiceResult<T>(ResultType.Unavailable, Error: error);
    }

    public static ServiceResult<T> Failure(string error)
    {
        return new ServiceResult<T>(ResultType.Failure, Error: error);
    }
}

public sealed record ServiceResult(ResultType ResultType, string? Error = null)
{
    public bool Succeeded => ResultType is ResultType.Success or ResultType.Created or ResultType.NoContent;

    public static ServiceResult Success()
    {
        return new ServiceResult(ResultType.Success);
    }

    public static ServiceResult Created()
    {
        return new ServiceResult(ResultType.Created);
    }

    public static ServiceResult NoContent()
    {
        return new ServiceResult(ResultType.NoContent);
    }

    public static ServiceResult ValidationError(string error)
    {
        return new ServiceResult(ResultType.ValidationError, error);
    }

    public static ServiceResult Unauthorized(string? error = null)
    {
        return new ServiceResult(ResultType.Unauthorized, error);
    }

    public static ServiceResult Forbidden(string? error = null)
    {
        return new ServiceResult(ResultType.Forbidden, error);
    }

    public static ServiceResult NotFound(string? error = null)
    {
        return new ServiceResult(ResultType.NotFound, error);
    }

    public static ServiceResult Conflict(string error)
    {
        return new ServiceResult(ResultType.Conflict, error);
    }

    public static ServiceResult Unavailable(string error)
    {
        return new ServiceResult(ResultType.Unavailable, error);
    }

    public static ServiceResult Failure(string error)
    {
        return new ServiceResult(ResultType.Failure, error);
    }
}
