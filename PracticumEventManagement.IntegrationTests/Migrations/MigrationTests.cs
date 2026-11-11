
using Microsoft.EntityFrameworkCore;
using PracticumEventManagement.IntegrationTests.Fixtures;
using Xunit;

namespace PracticumEventManagement.IntegrationTests.Migrations;

[Collection("PostgreSQL")]
public class MigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public MigrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task InitialCreate_ShouldCreateTablesAndForeignKey()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();

        // Act
        var appliedMigrations = await context.Database
            .GetAppliedMigrationsAsync();

        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        // Assert
        Assert.Contains(
            appliedMigrations,
            migration => migration.EndsWith("_InitialCreate"));

        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_name IN ('events', 'bookings');
            """;

        var tableCount = Convert.ToInt32(
            await command.ExecuteScalarAsync());

        Assert.Equal(2, tableCount);

        command.CommandText = """
            SELECT COUNT(*)
            FROM pg_constraint
            WHERE conname = 'FK_bookings_events_event_id'
              AND contype = 'f';
            """;

        var foreignKeyCount = Convert.ToInt32(
            await command.ExecuteScalarAsync());

        Assert.Equal(1, foreignKeyCount);
    }
}
