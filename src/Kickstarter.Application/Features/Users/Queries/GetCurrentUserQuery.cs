using System.Security.Claims;
using Kickstarter.Application.Contracts.Users;
using Kickstarter.Application.Results;
using MediatR;

namespace Kickstarter.Application.Features.Users.Queries;

public sealed record GetCurrentUserQuery(ClaimsPrincipal Principal) : IRequest<ServiceResult<UserResponse>>;
