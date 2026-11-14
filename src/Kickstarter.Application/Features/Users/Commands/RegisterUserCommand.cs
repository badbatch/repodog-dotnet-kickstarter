using Kickstarter.Application.Contracts.Users;
using Kickstarter.Application.Results;
using MediatR;

namespace Kickstarter.Application.Features.Users.Commands;

public sealed record RegisterUserCommand(string Email, string Password, string? UserName)
    : IRequest<ServiceResult<UserResponse>>;
