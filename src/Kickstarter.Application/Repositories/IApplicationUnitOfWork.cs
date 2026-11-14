namespace Kickstarter.Application.Repositories;

public interface IApplicationUnitOfWork
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken);
}
