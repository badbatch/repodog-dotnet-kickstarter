using System.Security.Claims;
using Kickstarter.Api.Authorization;
using Kickstarter.Application.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Kickstarter.IntegrationTests;

public sealed class AuthorizationPolicyTests
{
    [Theory]
    [InlineData(false, "ExampleUser", false)]
    [InlineData(true, "Customer", false)]
    [InlineData(true, "ExampleUser", true)]
    public async Task GivenConfigure_WhenExampleAccessIsEvaluated_ThenOnlyAuthenticatedExampleUsersSucceed(
        bool authenticated,
        string role,
        bool expected
    )
    {
        // Arrange
        await using var services = new ServiceCollection()
            .AddLogging()
            .AddAuthorization(AuthorizationPolicies.Configure)
            .BuildServiceProvider();

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], authenticated ? "Test" : null)
        );

        var authorization = services.GetRequiredService<IAuthorizationService>();

        // Act
        var result = await authorization.AuthorizeAsync(principal, null, AuthorizationPolicies.ExampleAccess);

        // Assert
        result.Succeeded.ShouldBe(expected);
    }

    [Theory]
    [InlineData("ExampleUser", true)]
    [InlineData("Customer", false)]
    public async Task GivenCreateAsync_WhenCurrentUserIsCreated_ThenRolesAndEvaluatedPoliciesAreIncluded(
        string role,
        bool privileged
    )
    {
        // Arrange
        await using var services = new ServiceCollection()
            .AddLogging()
            .AddAuthorization(AuthorizationPolicies.Configure)
            .BuildServiceProvider();

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Role, role), new Claim(ClaimTypes.Role, role)], "Test")
        );

        var factory = new CurrentUserResponseFactory(services.GetRequiredService<IAuthorizationService>());
        var user = new UserResponse(Guid.NewGuid(), "test", "test@example.test");

        // Act
        var response = await factory.CreateAsync(user, principal);

        // Assert
        response.Id.ShouldBe(user.Id);
        response.Roles.ShouldBe([role]);
        response.Policies.ShouldBe(privileged ? [AuthorizationPolicies.ExampleAccess] : Array.Empty<string>());
    }
}
