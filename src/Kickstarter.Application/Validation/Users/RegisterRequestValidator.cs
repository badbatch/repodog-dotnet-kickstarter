using FluentValidation;
using Kickstarter.Application.Contracts.Users;

namespace Kickstarter.Application.Validation.Users;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(request => request.Password).NotEmpty();
        RuleFor(request => request.UserName).MaximumLength(256).When(request => request.UserName is not null);
    }
}
