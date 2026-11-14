using FluentValidation;
using Kickstarter.Application.Contracts.Users;

namespace Kickstarter.Application.Validation.Users;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress();
        RuleFor(request => request.Password).NotEmpty();
    }
}
