using Kickstarter.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kickstarter.IntegrationTests.Setup;

public sealed class IntegrationTestHostBuilder(string dbConnectionString, ITestOutputHelper testOutputHelper)
    : IAsyncDisposable
{
    private readonly List<Action<IConfigurationBuilder>> _configureAppConfigurationList = [];
    private readonly List<Action<IServiceCollection>> _configureServicesList = [];

    private readonly Dictionary<
        Type,
        (Uri BaseUri, MockHttpMessageHandler MockHttpMessageHandler)
    > _httpClientMocksDictionary = [];

    private CustomWebApplicationFactory<ApplicationDbContext>? _factory;

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    public IntegrationTestHostBuilder WithClient<TTypedClient>(
        Uri baseUri,
        MockHttpMessageHandler mockHttpMessageHandler
    )
    {
        EnsureFactoryNotCreated();
        _httpClientMocksDictionary[typeof(TTypedClient)] = (baseUri, mockHttpMessageHandler);
        return this;
    }

    public IntegrationTestHostBuilder WithConfigureServices(Action<IServiceCollection> configureServices)
    {
        EnsureFactoryNotCreated();
        _configureServicesList.Add(configureServices);
        return this;
    }

    public IntegrationTestHostBuilder WithConfigureAppConfiguration(
        Action<IConfigurationBuilder> configureConfiguration
    )
    {
        EnsureFactoryNotCreated();
        _configureAppConfigurationList.Add(configureConfiguration);
        return this;
    }

    public async Task WithDbContextAsync(Func<ApplicationDbContext, Task> action)
    {
        var factory = GetFactory();
        var services = factory.Services;
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await action(context);
    }

    public async Task<TResult> WithServicesAsync<TResult>(Func<IServiceProvider, Task<TResult>> action)
    {
        var services = GetFactory().Services;
        await using var scope = services.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    public HttpClient Build()
    {
        return GetFactory().CreateClientWithDefaults();
    }

    private CustomWebApplicationFactory<ApplicationDbContext> GetFactory()
    {
        return _factory ??= CreateFactory();
    }

    private CustomWebApplicationFactory<ApplicationDbContext> CreateFactory()
    {
        return new CustomWebApplicationFactory<ApplicationDbContext>(
            dbConnectionString,
            testOutputHelper,
            services =>
            {
                foreach (var typedClient in _httpClientMocksDictionary)
                {
                    services.PostConfigure<HttpClientFactoryOptions>(
                        typedClient.Key.Name,
                        options =>
                        {
                            options.HttpMessageHandlerBuilderActions.Add(builder =>
                                builder.PrimaryHandler = typedClient.Value.MockHttpMessageHandler
                            );

                            options.HttpClientActions.Add(client => client.BaseAddress = typedClient.Value.BaseUri);
                        }
                    );
                }

                foreach (var configureServices in _configureServicesList)
                {
                    configureServices(services);
                }
            },
            configuration =>
            {
                foreach (var configureAppConfiguration in _configureAppConfigurationList)
                {
                    configureAppConfiguration(configuration);
                }
            }
        );
    }

    private void EnsureFactoryNotCreated()
    {
        if (_factory is not null)
        {
            throw new InvalidOperationException(
                "The test host has already been created and can no longer be configured."
            );
        }
    }
}
