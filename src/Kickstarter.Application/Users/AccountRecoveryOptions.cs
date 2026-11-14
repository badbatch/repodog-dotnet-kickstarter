namespace Kickstarter.Application.Users;

public sealed class AccountRecoveryOptions
{
    public const string SectionName = "AccountRecovery";

    public string PasswordResetUrl { get; set; } = string.Empty;
}
