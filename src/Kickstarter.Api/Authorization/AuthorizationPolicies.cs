using Microsoft.AspNetCore.Authorization;

namespace Kickstarter.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string ExampleAccess = "ExampleAccess";

    // Only resource-independent policies may be exposed in the current-user response.
    public static IReadOnlyList<string> CurrentUserPolicies { get; } = Array.AsReadOnly([ExampleAccess]);

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(ExampleAccess, policy => policy.RequireAuthenticatedUser().RequireRole("ExampleUser"));
    }
}
