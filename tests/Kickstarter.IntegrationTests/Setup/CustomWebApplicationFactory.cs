using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Kickstarter.IntegrationTests.Setup;

public sealed class CustomWebApplicationFactory<TDbContext>(
    string connectionString,
    ITestOutputHelper testOutputHelper,
    Action<IServiceCollection>? configureServices = null,
    Action<IConfigurationBuilder>? configureAppConfiguration = null
) : WebApplicationFactory<Program>
    where TDbContext : DbContext
{
    private const string RequestIdHeaderKey = "X-Request-Id";
    private const string CorrelationIdHeaderKey = "X-Correlation-Id";

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(
            (_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(
                    new Dictionary<string, string?> { ["ConnectionStrings:ApplicationDb"] = connectionString }
                );

                configureAppConfiguration?.Invoke(configBuilder);
            }
        );

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services => configureServices?.Invoke(services));

        builder.ConfigureLogging(loggingBuilder =>
        {
            loggingBuilder.AddFilter("Microsoft", LogLevel.Warning);
            loggingBuilder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            loggingBuilder.AddProvider(new TestOutputLoggerProvider(testOutputHelper));
        });
    }

    public HttpClient CreateClientWithDefaults()
    {
        var client = CreateClient();
        client.Timeout = TimeSpan.FromMinutes(10);
        client.DefaultRequestHeaders.Add(RequestIdHeaderKey, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(CorrelationIdHeaderKey, Guid.NewGuid().ToString());
        return client;
    }

    private sealed class TestOutputLoggerProvider(ITestOutputHelper testOutputHelper) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new TestOutputLogger(categoryName, testOutputHelper);
        }

        public void Dispose() { }
    }

    private sealed class TestOutputLogger(string categoryName, ITestOutputHelper testOutputHelper) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            try
            {
                testOutputHelper.WriteLine($"[{logLevel}] {categoryName}: {formatter(state, exception)}");

                if (exception is not null)
                {
                    testOutputHelper.WriteLine(exception.ToString());
                }
            }
            catch (InvalidOperationException)
            {
                // Test output can be unavailable when a background service logs as a test ends.
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose() { }
    }
}
