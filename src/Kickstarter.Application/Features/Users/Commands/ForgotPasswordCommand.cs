using Kickstarter.Application.Results;
using MediatR;

namespace Kickstarter.Application.Features.Users.Commands;

public sealed record ForgotPasswordCommand(string Email) : IRequest<ServiceResult>;
