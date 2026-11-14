using Kickstarter.Application.Results;
using MediatR;

namespace Kickstarter.Application.Features.Health.Queries;

public sealed record CheckDatabaseHealthQuery : IRequest<ServiceResult<string>>;
