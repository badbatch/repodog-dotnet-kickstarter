using System.Net.Http.Json;
using Kickstarter.Infrastructure.Data;
using Kickstarter.TestCommon;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Kickstarter.IntegrationTests.Setup;

public abstract class BaseTest(SqlServerFixture sqlServerFixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<SqlServerFixture>,
        IAsyncLifetime
{
    private bool _isTestHostBuilderSetup;

    private HttpClient? _testClient;
    protected FakeTimeProvider Clock { get; } = new();

    private IntegrationTestHostBuilder IntegrationTestHostBuilder { get; } =
        new(CreateTestDatabaseConnectionString(sqlServerFixture), testOutputHelper);

    protected HttpClient TestClient
    {
        get
        {
            EnsureTestHostBuilderSetup();
            return _testClient ??= IntegrationTestHostBuilder.Build();
        }
    }

    public virtual ValueTask InitializeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public virtual async ValueTask DisposeAsync()
    {
        _testClient?.Dispose();
        await IntegrationTestHostBuilder.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected async Task SetupDbContextAsync(Func<ApplicationDbContext, Task> action)
    {
        EnsureTestHostBuilderSetup();
        await IntegrationTestHostBuilder.WithDbContextAsync(action);
    }

    protected async Task InspectDbContextAsync(Func<ApplicationDbContext, Task> action)
    {
        EnsureTestHostBuilderSetup();
        await IntegrationTestHostBuilder.WithDbContextAsync(action);
    }

    protected Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        EnsureTestHostBuilderSetup();

        return IntegrationTestHostBuilder.WithServicesAsync(serviceProvider =>
            serviceProvider.GetRequiredService<IMediator>().Send(request, TestContext.Current.CancellationToken)
        );
    }

    protected static async Task<T> ReadFromJsonAsync<T>(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<T>(
                TestJsonOptions.Default,
                TestContext.Current.CancellationToken
            ) ?? throw new InvalidOperationException($"Response body could not be deserialized to '{typeof(T).Name}'.");
    }

    protected virtual void SetupTestHostBuilder(IntegrationTestHostBuilder builder) { }

    private void EnsureTestHostBuilderSetup()
    {
        if (_isTestHostBuilderSetup)
        {
            return;
        }

        IntegrationTestHostBuilder.WithConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });

        SetupTestHostBuilder(IntegrationTestHostBuilder);
        _isTestHostBuilderSetup = true;
    }

    private static string CreateTestDatabaseConnectionString(SqlServerFixture sqlServerFixture)
    {
        var connectionString = new SqlConnectionStringBuilder(sqlServerFixture.DbContainer.GetConnectionString())
        {
            InitialCatalog = $"KickstarterIntegration_{Guid.NewGuid():N}",
        };

        return connectionString.ConnectionString;
    }
}
