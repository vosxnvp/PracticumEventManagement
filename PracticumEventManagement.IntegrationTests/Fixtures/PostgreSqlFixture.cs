
using Microsoft.EntityFrameworkCore;
using PracticumEventManagement.DataAccess;
using Testcontainers.PostgreSql;
using Xunit;

namespace PracticumEventManagement.IntegrationTests.Fixtures;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("eventapi_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateDbContext();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }
}
