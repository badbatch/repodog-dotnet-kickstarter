using System.Net;
using System.Text.Json;
using Kickstarter.IntegrationTests.Setup;
using Kickstarter.TestCommon;
using Kickstarter.TestCommon.Faker.Builders.Dto;
using Kickstarter.TestCommon.Faker.Builders.Entity;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Kickstarter.IntegrationTests;

public sealed class IdentityAndHealthIntegrationTests(SqlServerFixture fixture, ITestOutputHelper testOutputHelper)
    : BaseTest(fixture, testOutputHelper)
{
    private const string RegisterPassword = "P@ssword12345";
    private static readonly Guid UserId = Guid.Parse("7c9f0b2e-8d51-4b78-a313-1db51e6b0f20");

    protected override void SetupTestHostBuilder(IntegrationTestHostBuilder builder)
    {
        builder.WithIntegrationTestAuthentication();
    }

    [Fact]
    public async Task GivenValidRegistration_WhenRegistered_ThenCreateUser()
    {
        // Arrange
        var request = new FakeRegisterRequest()
            .WithEmail($"new-{Guid.NewGuid():N}@example.test")
            .WithPassword(RegisterPassword)
            .Generate();

        // Act
        using var response = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/register",
            request,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        await InspectDbContextAsync(async db =>
        {
            (
                await db.Users.AnyAsync(user => user.Email == request.Email, TestContext.Current.CancellationToken)
            ).ShouldBeTrue();
        });
    }

    [Fact]
    public async Task GivenInvalidRegistration_WhenRegistered_ThenReturnBadRequest()
    {
        // Arrange
        var request = new FakeRegisterRequest().WithEmail("not-an-email").Generate();

        // Act
        using var response = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/register",
            request,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenRegisteredUser_WhenLoginRequested_ThenReturnBearerTokens()
    {
        // Arrange
        var email = $"login-{Guid.NewGuid():N}@example.test";
        var registration = new FakeRegisterRequest().WithEmail(email).WithPassword(RegisterPassword).Generate();

        using var registrationResponse = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/register",
            registration,
            TestContext.Current.CancellationToken
        );

        var request = new FakeLoginRequest().WithEmail(email).WithPassword(RegisterPassword).Generate();

        // Act
        using var response = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/login",
            request,
            TestContext.Current.CancellationToken
        );

        var tokens = await ReadFromJsonAsync<JsonElement>(response);

        // Assert
        registrationResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        tokens.GetProperty("tokenType").GetString().ShouldBe("Bearer");
        tokens.GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();
        tokens.GetProperty("refreshToken").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GivenInvalidCredentials_WhenLoginRequested_ThenReturnUnauthorized()
    {
        // Arrange
        var request = new FakeLoginRequest().WithEmail($"missing-{Guid.NewGuid():N}@example.test").Generate();

        // Act
        using var response = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/login",
            request,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GivenValidRefreshToken_WhenRefreshed_ThenReturnNewBearerTokens()
    {
        // Arrange
        var email = $"refresh-{Guid.NewGuid():N}@example.test";
        var registration = new FakeRegisterRequest().WithEmail(email).WithPassword(RegisterPassword).Generate();
        using var registrationResponse = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/register",
            registration,
            TestContext.Current.CancellationToken
        );
        var login = new FakeLoginRequest().WithEmail(email).WithPassword(RegisterPassword).Generate();
        using var loginResponse = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/login",
            login,
            TestContext.Current.CancellationToken
        );
        var loginTokens = await ReadFromJsonAsync<JsonElement>(loginResponse);
        var refreshToken = loginTokens.GetProperty("refreshToken").GetString();

        // Act
        using var response = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/refresh",
            new { refreshToken },
            TestContext.Current.CancellationToken
        );
        var refreshedTokens = await ReadFromJsonAsync<JsonElement>(response);

        // Assert
        registrationResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        refreshedTokens.GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenCurrentUserRequested_ThenReturnUserProfile()
    {
        // Arrange
        await SetupDbContextAsync(db =>
        {
            db.Users.Add(
                new FakeApplicationUser()
                    .WithId(UserId)
                    .WithUserName(UserId.ToString())
                    .WithEmail("integration@example.test")
                    .Generate()
            );

            return db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        // Act
        using var response = await TestClient.GetAsync("/api/v1/users/me", TestContext.Current.CancellationToken);
        var user = await ReadFromJsonAsync<JsonElement>(response);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        user.GetProperty("id").GetGuid().ShouldBe(UserId);
    }

    [Fact]
    public async Task GivenHealthEndpoint_WhenRequested_ThenReturnHealthy()
    {
        // Arrange

        // Act
        using var response = await TestClient.GetAsync("/health", TestContext.Current.CancellationToken);
        var health = await ReadFromJsonAsync<JsonElement>(response);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        health.GetProperty("status").GetString().ShouldBe("healthy");
    }

    [Fact]
    public async Task GivenDatabaseHealthEndpoint_WhenRequested_ThenReturnHealthy()
    {
        // Arrange

        // Act
        using var response = await TestClient.GetAsync("/health/database", TestContext.Current.CancellationToken);
        var health = await ReadFromJsonAsync<JsonElement>(response);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        health.GetProperty("status").GetString().ShouldBe("healthy");
    }
}
