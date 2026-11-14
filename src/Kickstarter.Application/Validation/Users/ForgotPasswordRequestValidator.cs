using FluentValidation;
using Kickstarter.Application.Contracts.Users;

namespace Kickstarter.Application.Validation.Users;

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
