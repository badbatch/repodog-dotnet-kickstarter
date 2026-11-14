using System.Security.Claims;
using Kickstarter.Application.Users;
using Kickstarter.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Kickstarter.Infrastructure.Users;

public sealed class IdentityRepository(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signInManager,
    IOptionsMonitor<BearerTokenOptions> bearerTokenOptions,
    TimeProvider timeProvider
) : IIdentityRepository
{
    public async Task<IReadOnlyList<string>> CreateUserAsync(ApplicationUser user, string password)
    {
        var result = await users.CreateAsync(user, password);
        return result.Succeeded ? [] : result.Errors.Select(error => error.Description).ToArray();
    }

    public async Task<ClaimsPrincipal?> SignInAsync(string email, string password)
    {
        var user = await users.FindByEmailAsync(email);

        if (user is null || !(await signInManager.CheckPasswordSignInAsync(user, password, true)).Succeeded)
        {
            return null;
        }

        return await signInManager.CreateUserPrincipalAsync(user);
    }

    public async Task<ClaimsPrincipal?> RefreshPrincipalAsync(string refreshToken)
    {
        var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
        var ticket = refreshTokenProtector.Unprotect(refreshToken);

        if (
            ticket?.Properties?.ExpiresUtc is not { } expiresUtc
            || timeProvider.GetUtcNow() >= expiresUtc
            || await signInManager.ValidateSecurityStampAsync(ticket.Principal) is not { } user
        )
        {
            return null;
        }

        return await signInManager.CreateUserPrincipalAsync(user);
    }

    public Task<ApplicationUser?> GetUserAsync(ClaimsPrincipal principal)
    {
        return users.GetUserAsync(principal);
    }

    public async Task<(string Email, string Token)?> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(email);
        if (user is null || user.Email is null)
        {
            return null;
        }

        var token = await users.GeneratePasswordResetTokenAsync(user);
        cancellationToken.ThrowIfCancellationRequested();
        return (user.Email, token);
    }

    public async Task<IReadOnlyList<string>> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            return ["The password reset request is invalid or expired."];
        }

        var result = await users.ResetPasswordAsync(user, token, newPassword);
        cancellationToken.ThrowIfCancellationRequested();
        return result.Succeeded ? [] : result.Errors.Select(error => error.Description).ToArray();
    }
}
