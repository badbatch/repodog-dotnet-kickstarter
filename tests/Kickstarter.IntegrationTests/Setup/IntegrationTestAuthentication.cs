using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kickstarter.IntegrationTests.Setup;

public static class IntegrationTestAuthentication
{
    public static IntegrationTestHostBuilder WithIntegrationTestAuthentication(this IntegrationTestHostBuilder builder)
    {
        builder.WithConfigureServices(services =>
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "IntegrationTest";
                    options.DefaultChallengeScheme = "IntegrationTest";
                })
                .AddScheme<AuthenticationSchemeOptions, IntegrationAuthHandler>("IntegrationTest", _ => { });
        });

        return builder;
    }

    private sealed class IntegrationAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder
    ) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "7c9f0b2e-8d51-4b78-a313-1db51e6b0f20"),
                    new Claim(ClaimTypes.Role, "ExampleUser"),
                ],
                "IntegrationTest"
            );

            return Task.FromResult(
                AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name))
            );
        }
    }
}
