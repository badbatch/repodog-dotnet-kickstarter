using System.Security.Claims;
using Kickstarter.Domain.Entities;

namespace Kickstarter.Application.Users;

public interface IIdentityRepository
{
    Task<IReadOnlyList<string>> CreateUserAsync(ApplicationUser user, string password);

    Task<ClaimsPrincipal?> SignInAsync(string email, string password);

    Task<ClaimsPrincipal?> RefreshPrincipalAsync(string refreshToken);

    Task<ApplicationUser?> GetUserAsync(ClaimsPrincipal principal);

    Task<(string Email, string Token)?> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<string>> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default
    );
}
