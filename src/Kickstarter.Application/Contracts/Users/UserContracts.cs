namespace Kickstarter.Application.Contracts.Users;

public sealed record RegisterRequest(string Email, string Password, string? UserName);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

public sealed record UserResponse(Guid Id, string? UserName, string? Email);

public sealed record CurrentUserResponse(
    Guid Id,
    string? UserName,
    string? Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Policies
);
