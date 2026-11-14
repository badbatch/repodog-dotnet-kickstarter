namespace Kickstarter.Infrastructure.Users;

public sealed class SmtpEmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool UseTls { get; set; }
    public string? UserName { get; set; }
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "no-reply@kickstarter.local";
    public string FromName { get; set; } = "Kickstarter";
}
