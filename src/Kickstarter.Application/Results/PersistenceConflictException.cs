namespace Kickstarter.Application.Results;

public sealed class PersistenceConflictException(Exception innerException)
    : Exception("A database constraint prevented the requested change.", innerException);
