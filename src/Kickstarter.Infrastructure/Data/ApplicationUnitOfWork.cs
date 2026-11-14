using Kickstarter.Application.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kickstarter.Infrastructure.Data;

public sealed class ApplicationUnitOfWork(ApplicationDbContext db) : IApplicationUnitOfWork
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        return db.Database.CanConnectAsync(cancellationToken);
    }
}
