using Testcontainers.MsSql;
using Xunit;

namespace Kickstarter.TestCommon;

public sealed class SqlServerFixture : IAsyncLifetime
{
    public MsSqlContainer DbContainer { get; }

    public SqlServerFixture()
    {
        DbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Your_strong_password123")
            .Build();
    }

    public async ValueTask InitializeAsync()
    {
        await DbContainer.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await DbContainer.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public async ValueTask PauseAsync()
    {
        await DbContainer.PauseAsync();
    }
}
