using FluentValidation;
using Kickstarter.Application.Contracts.Users;

namespace Kickstarter.Application.Validation.Users;

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(request => request.RefreshToken).NotEmpty();
    }
}
