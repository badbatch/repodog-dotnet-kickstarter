using Asp.Versioning;
using FluentValidation;
using Kickstarter.Application.Contracts.Users;
using Kickstarter.Application.Features.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Kickstarter.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(
    IMediator mediator,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshTokenRequest> refreshTokenValidator,
    IValidator<ForgotPasswordRequest> forgotPasswordValidator,
    IValidator<ResetPasswordRequest> resetPasswordValidator
) : ApiControllerBase
{
    [HttpPost("register", Name = "registerUser")]
    public async Task<Results<Created<UserResponse>, ProblemHttpResult>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken
    )
    {
        if (await ValidateAsync(request, registerValidator, cancellationToken) is { } validationProblem)
        {
            return validationProblem;
        }

        var result = await mediator.Send(
            new RegisterUserCommand(request.Email, request.Password, request.UserName),
            cancellationToken
        );

        return ToCreatedResult(result, "/api/v1/users/me");
    }

    [HttpPost("login", Name = "loginUser")]
    public async Task<Results<SignInHttpResult, ProblemHttpResult>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        if (await ValidateAsync(request, loginValidator, cancellationToken) is { } validationProblem)
        {
            return validationProblem;
        }

        var result = await mediator.Send(new LoginUserCommand(request.Email, request.Password), cancellationToken);

        return result.Value is { } principal
            ? TypedResults.SignIn(principal, authenticationScheme: IdentityConstants.BearerScheme)
            : TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials.");
    }

    [HttpPost("refresh", Name = "refreshUserToken")]
    public async Task<Results<SignInHttpResult, ProblemHttpResult>> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken
    )
    {
        if (await ValidateAsync(request, refreshTokenValidator, cancellationToken) is { } validationProblem)
        {
            return validationProblem;
        }

        var result = await mediator.Send(new RefreshUserTokenCommand(request.RefreshToken), cancellationToken);

        return result.Value is { } principal
            ? TypedResults.SignIn(principal, authenticationScheme: IdentityConstants.BearerScheme)
            : TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid refresh token.");
    }

    [HttpPost("forgot-password", Name = "forgotPassword")]
    public async Task<Results<NoContent, ProblemHttpResult>> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken
    )
    {
        if (await ValidateAsync(request, forgotPasswordValidator, cancellationToken) is { } validationProblem)
        {
            return validationProblem;
        }

        return ToNoContentResult(await mediator.Send(new ForgotPasswordCommand(request.Email), cancellationToken));
    }

    [HttpPost("reset-password", Name = "resetPassword")]
    public async Task<Results<NoContent, ProblemHttpResult>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken
    )
    {
        if (await ValidateAsync(request, resetPasswordValidator, cancellationToken) is { } validationProblem)
        {
            return validationProblem;
        }

        return ToNoContentResult(
            await mediator.Send(
                new ResetPasswordCommand(request.Email, request.Token, request.NewPassword),
                cancellationToken
            )
        );
    }
}
