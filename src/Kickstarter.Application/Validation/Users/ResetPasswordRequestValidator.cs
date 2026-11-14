using FluentValidation;
using Kickstarter.Application.Contracts.Users;

namespace Kickstarter.Application.Validation.Users;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(request => request.Token).NotEmpty().MaximumLength(4096);
        RuleFor(request => request.NewPassword).NotEmpty().MaximumLength(256);
    }
}
