using Kickstarter.Application.Features.Health.Queries;
using Kickstarter.Application.Repositories;
using Kickstarter.Application.Results;
using MediatR;

namespace Kickstarter.Application.Features.Health.Handlers;

public sealed class CheckDatabaseHealthHandler(IApplicationUnitOfWork unitOfWork)
    : IRequestHandler<CheckDatabaseHealthQuery, ServiceResult<string>>
{
    public async Task<ServiceResult<string>> Handle(
        CheckDatabaseHealthQuery request,
        CancellationToken cancellationToken
    )
    {
        return await unitOfWork.CanConnectAsync(cancellationToken)
            ? ServiceResult<string>.Success("connected")
            : ServiceResult<string>.Unavailable("Database is unavailable.");
    }
}
