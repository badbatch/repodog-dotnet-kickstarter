using Kickstarter.Application.Results;
using MediatR;

namespace Kickstarter.Application.Features.Users.Commands;

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<ServiceResult>;
