using System.Collections.Concurrent;
using System.Net;
using Kickstarter.Application.Users;
using Kickstarter.IntegrationTests.Setup;
using Kickstarter.TestCommon;
using Kickstarter.TestCommon.Faker.Builders.Dto;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Xunit;

namespace Kickstarter.IntegrationTests;

public sealed class AccountRecoveryIntegrationTests(SqlServerFixture fixture, ITestOutputHelper testOutputHelper)
    : BaseTest(fixture, testOutputHelper)
{
    private const string ExistingPassword = "P@ssword12345";
    private const string UpdatedPassword = "N3w-P@ssword12345";
    private readonly FakeEmailSender _emailSender = new();

    protected override void SetupTestHostBuilder(IntegrationTestHostBuilder builder)
    {
        builder.WithConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(_emailSender);
        });
    }

    [Fact]
    public async Task GivenExistingUser_WhenPasswordResetRequested_ThenSendResetEmail()
    {
        // Arrange
        var email = $"recovery-{Guid.NewGuid():N}@example.test";
        var registration = new FakeRegisterRequest().WithEmail(email).WithPassword(ExistingPassword).Generate();
        using var registrationResponse = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/register",
            registration,
            TestContext.Current.CancellationToken
        );
        var request = new { Email = email };

        // Act
        using var response = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/forgot-password",
            request,
            TestContext.Current.CancellationToken
        );

        // Assert
        registrationResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        _emailSender.Messages.ShouldContain(message =>
            message.Recipient == email && message.Subject.Contains("Reset", StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task GivenValidPasswordResetToken_WhenPasswordReset_ThenAllowLoginWithNewPassword()
    {
        // Arrange
        var email = $"reset-{Guid.NewGuid():N}@example.test";
        var registration = new FakeRegisterRequest().WithEmail(email).WithPassword(ExistingPassword).Generate();
        using var registrationResponse = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/register",
            registration,
            TestContext.Current.CancellationToken
        );
        using var requestResponse = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/forgot-password",
            new { Email = email },
            TestContext.Current.CancellationToken
        );
        var resetLink = _emailSender
            .Messages.Single()
            .TextBody.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Last();
        var resetQuery = QueryHelpers.ParseQuery(new Uri(resetLink).Query);

        // Act
        using var resetResponse = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/reset-password",
            new
            {
                Email = resetQuery["email"].ToString(),
                Token = resetQuery["token"].ToString(),
                NewPassword = UpdatedPassword,
            },
            TestContext.Current.CancellationToken
        );
        using var loginResponse = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/login",
            new { Email = email, Password = UpdatedPassword },
            TestContext.Current.CancellationToken
        );

        // Assert
        registrationResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        requestResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        resetResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GivenUnknownEmail_WhenPasswordResetRequested_ThenReturnSameResponseWithoutSendingEmail()
    {
        // Arrange
        var request = new { Email = $"unknown-{Guid.NewGuid():N}@example.test" };

        // Act
        using var response = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/forgot-password",
            request,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        _emailSender.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task GivenInvalidPasswordResetToken_WhenPasswordReset_ThenReturnBadRequest()
    {
        // Arrange
        var request = new
        {
            Email = $"invalid-{Guid.NewGuid():N}@example.test",
            Token = "invalid-token",
            NewPassword = UpdatedPassword,
        };

        // Act
        using var response = await TestClient.PostAsJsonWithDefaultsAsync(
            "/api/v1/auth/reset-password",
            request,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private sealed record SentMessage(string Recipient, string Subject, string TextBody);

    private sealed class FakeEmailSender : IEmailSender
    {
        private readonly ConcurrentQueue<SentMessage> _messages = new();

        public IReadOnlyCollection<SentMessage> Messages => _messages.ToArray();

        public Task SendAsync(
            string recipient,
            string subject,
            string textBody,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            _messages.Enqueue(new SentMessage(recipient, subject, textBody));
            return Task.CompletedTask;
        }
    }
}
