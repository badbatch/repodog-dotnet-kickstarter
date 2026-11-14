using System.Security.Claims;
using Kickstarter.Application.Results;
using MediatR;

namespace Kickstarter.Application.Features.Users.Commands;

public sealed record RefreshUserTokenCommand(string RefreshToken) : IRequest<ServiceResult<ClaimsPrincipal>>;
