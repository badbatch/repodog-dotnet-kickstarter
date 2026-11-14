using System.Net;
using System.Net.Http.Headers;
using Kickstarter.Api.Authorization;
using Kickstarter.Application.Contracts.Users;
using Kickstarter.IntegrationTests.Setup;
using Kickstarter.TestCommon;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Kickstarter.IntegrationTests;

public sealed class CurrentUserAccessIntegrationTests(SqlServerFixture fixture, ITestOutputHelper output)
    : BaseTest(fixture, output)
{
    [Fact]
    public async Task GivenGetMeAsync_WhenRequestedWithoutAuthentication_ThenReturnUnauthorized()
    {
        // Arrange
        const string path = "/api/v1/users/me";

        // Act
        using var response = await TestClient.GetAsync(path, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GivenGetMeAsync_WhenUserSignsIn_ThenPoliciesMatchRestrictedEndpointAccess(bool privileged)
    {
        // Arrange
        var tokens = await SignInAsync(privileged);
        TestClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        // Act
        using var meResponse = await TestClient.GetAsync("/api/v1/users/me", TestContext.Current.CancellationToken);
        var me = await ReadFromJsonAsync<CurrentUserResponse>(meResponse);
        using var exampleResponse = await TestClient.GetAsync("/api/v1/example", TestContext.Current.CancellationToken);
        using var restrictedResponse = await TestClient.GetAsync(
            "/api/v1/example/restricted",
            TestContext.Current.CancellationToken
        );

        // Assert
        meResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        exampleResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        me.Roles.ShouldBe(privileged ? ["ExampleUser"] : Array.Empty<string>());
        me.Policies.ShouldBe(privileged ? [AuthorizationPolicies.ExampleAccess] : Array.Empty<string>());
        restrictedResponse.StatusCode.ShouldBe(privileged ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GivenRefreshAsync_WhenExampleRoleIsRemovedAndTokensAreRefreshed_ThenExampleAccessIsDenied()
    {
        // Arrange
        var tokens = await SignInAsync(true);
        await SetupDbContextAsync(async db =>
        {
            db.UserRoles.RemoveRange(await db.UserRoles.ToListAsync(TestContext.Current.CancellationToken));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        // Act
        using var refreshResponse = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(tokens.RefreshToken),
            TestContext.Current.CancellationToken
        );
        var refreshed = await ReadFromJsonAsync<BearerTokens>(refreshResponse);
        TestClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);
        using var meResponse = await TestClient.GetAsync("/api/v1/users/me", TestContext.Current.CancellationToken);
        var me = await ReadFromJsonAsync<CurrentUserResponse>(meResponse);
        using var exampleResponse = await TestClient.GetAsync("/api/v1/example", TestContext.Current.CancellationToken);
        using var restrictedResponse = await TestClient.GetAsync(
            "/api/v1/example/restricted",
            TestContext.Current.CancellationToken
        );

        // Assert
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        me.Roles.ShouldBeEmpty();
        me.Policies.ShouldBeEmpty();
        restrictedResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<BearerTokens> SignInAsync(bool privileged)
    {
        var email = $"access-{Guid.NewGuid():N}@example.test";
        const string password = "P@ssword12345";
        using var registration = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, password, email),
            TestContext.Current.CancellationToken
        );
        registration.StatusCode.ShouldBe(HttpStatusCode.Created);

        if (privileged)
        {
            await SetupDbContextAsync(async db =>
            {
                var user = await db.Users.SingleAsync(
                    user => user.Email == email,
                    TestContext.Current.CancellationToken
                );
                var role = await db.Roles.SingleAsync(
                    role => role.Name == "ExampleUser",
                    TestContext.Current.CancellationToken
                );
                db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = role.Id });
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            });
        }

        using var login = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, password),
            TestContext.Current.CancellationToken
        );
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await ReadFromJsonAsync<BearerTokens>(login);
    }

    private sealed record BearerTokens(string AccessToken, string RefreshToken);
}
