
using PracticumEventManagement.DataAccess.Repositories;
using PracticumEventManagement.IntegrationTests.Fixtures;
using PracticumEventManagement.Models;
using Xunit;

namespace PracticumEventManagement.IntegrationTests.Repositories;

[Collection("PostgreSQL")]
public class EventRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;


    private async Task SeedEventsAsync()
    {
        await using var context = _fixture.CreateDbContext();

        var repository = new EventRepository(context);

        var events = new[]
        {
        new Event("C# Конференция")
        {
            Id = Guid.NewGuid(),
            StartAt = new DateTime(2027, 1, 10, 10, 0, 0, DateTimeKind.Utc),
            EndAt = new DateTime(2027, 1, 10, 18, 0, 0, DateTimeKind.Utc),
            TotalSeats = 100,
            AvailableSeats = 100
        },
        new Event("ASP.NET Workshop")
        {
            Id = Guid.NewGuid(),
            StartAt = new DateTime(2027, 2, 15, 10, 0, 0, DateTimeKind.Utc),
            EndAt = new DateTime(2027, 2, 15, 18, 0, 0, DateTimeKind.Utc),
            TotalSeats = 50,
            AvailableSeats = 50
        },
        new Event("C# Advanced")
        {
            Id = Guid.NewGuid(),
            StartAt = new DateTime(2027, 3, 20, 10, 0, 0, DateTimeKind.Utc),
            EndAt = new DateTime(2027, 3, 20, 18, 0, 0, DateTimeKind.Utc),
            TotalSeats = 30,
            AvailableSeats = 30
        }
    };

        foreach (var eventItem in events)
        {
            await repository.AddAsync(eventItem);
        }

        await repository.SaveChangesAsync();
    }


    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEvents()
    {
        // Arrange
        await SeedEventsAsync();

        await using var context = _fixture.CreateDbContext();
        var repository = new EventRepository(context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(3, result.Items.Count());
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByTitleIgnoringCase()
    {
        // Arrange
        await SeedEventsAsync();

        await using var context = _fixture.CreateDbContext();
        var repository = new EventRepository(context);

        // Act
        var result = await repository.GetAllAsync(title: "c#");

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items,
            e => Assert.Contains("C#", e.Title));
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByFromDate()
    {
        // Arrange
        await SeedEventsAsync();

        await using var context = _fixture.CreateDbContext();
        var repository = new EventRepository(context);

        var from = new DateTime(
            2027, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await repository.GetAllAsync(from: from);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.DoesNotContain(result.Items,
            e => e.Title == "C# Конференция");
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByToDate()
    {
        // Arrange
        await SeedEventsAsync();

        await using var context = _fixture.CreateDbContext();
        var repository = new EventRepository(context);

        var to = new DateTime(
            2027, 2, 28, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await repository.GetAllAsync(to: to);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.DoesNotContain(result.Items,
            e => e.Title == "C# Advanced");
    }

    [Fact]
    public async Task GetAllAsync_ShouldApplyCombinedFilters()
    {
        // Arrange
        await SeedEventsAsync();

        await using var context = _fixture.CreateDbContext();
        var repository = new EventRepository(context);

        var from = new DateTime(
            2027, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        var to = new DateTime(
            2027, 3, 31, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await repository.GetAllAsync(
            title: "c#",
            from: from,
            to: to);

        // Assert
        var eventItem = Assert.Single(result.Items);

        Assert.Equal("C# Advanced", eventItem.Title);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_ShouldApplyPaginationAndSorting()
    {
        // Arrange
        await SeedEventsAsync();

        await using var context = _fixture.CreateDbContext();
        var repository = new EventRepository(context);

        // Act
        var firstPage = await repository.GetAllAsync(
            page: 1, pageSize: 2);

        var secondPage = await repository.GetAllAsync(
            page: 2, pageSize: 2);

        // Assert
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Count);
        Assert.Equal(1, firstPage.Page);

        Assert.Equal(3, secondPage.TotalCount);
        Assert.Equal(1, secondPage.Count);
        Assert.Equal(2, secondPage.Page);

        Assert.Equal(
            new[] { "C# Конференция", "ASP.NET Workshop" },
            firstPage.Items.Select(e => e.Title));

        Assert.Equal(
            "C# Advanced",
            Assert.Single(secondPage.Items).Title);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyResult_WhenNoMatches()
    {
        // Arrange
        await SeedEventsAsync();

        await using var context = _fixture.CreateDbContext();
        var repository = new EventRepository(context);

        // Act
        var result = await repository.GetAllAsync(
            title: "Несуществующее мероприятие");

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.Count);
    }

    public EventRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Repository_ShouldCreateReadUpdateAndDeleteEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var eventItem = new Event("Конференция .NET")
        {
            Id = eventId,
            Description = "Интеграционное тестирование",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 100,
            AvailableSeats = 100
        };

        // Act: Create
        await using (var context = _fixture.CreateDbContext())
        {
            var repository = new EventRepository(context);

            await repository.AddAsync(eventItem);
            await repository.SaveChangesAsync();
        }

        // Assert: Read
        await using (var context = _fixture.CreateDbContext())
        {
            var repository = new EventRepository(context);

            var savedEvent = await repository.GetByIdAsync(eventId);

            Assert.NotNull(savedEvent);
            Assert.Equal("Конференция .NET", savedEvent.Title);
            Assert.Equal(100, savedEvent.AvailableSeats);
        }

        // Act: Update
        await using (var context = _fixture.CreateDbContext())
        {
            var repository = new EventRepository(context);

            var trackedEvent = await repository
                .GetTrackedByIdAsync(eventId);

            Assert.NotNull(trackedEvent);

            trackedEvent.Title = "Обновлённая конференция";

            await repository.SaveChangesAsync();
        }

        // Assert: Update persisted
        await using (var context = _fixture.CreateDbContext())
        {
            var repository = new EventRepository(context);

            var updatedEvent = await repository.GetByIdAsync(eventId);

            Assert.NotNull(updatedEvent);
            Assert.Equal(
                "Обновлённая конференция",
                updatedEvent.Title);
        }

        // Act: Delete
        await using (var context = _fixture.CreateDbContext())
        {
            var repository = new EventRepository(context);

            var trackedEvent = await repository
                .GetTrackedByIdAsync(eventId);

            Assert.NotNull(trackedEvent);

            repository.Remove(trackedEvent);
            await repository.SaveChangesAsync();
        }

        // Assert: Delete persisted
        await using (var context = _fixture.CreateDbContext())
        {
            var repository = new EventRepository(context);

            var deletedEvent = await repository.GetByIdAsync(eventId);

            Assert.Null(deletedEvent);
        }
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenEventDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new EventRepository(context);

        var nonexistentId = Guid.NewGuid();

        // Act
        var result = await repository.GetByIdAsync(nonexistentId);

        // Assert
        Assert.Null(result);
    }


    [Fact]
    public async Task GetByIdAsync_ShouldNotTrackEntity()
    {
        // Arrange
        await SeedEventsAsync();

        await using var context = _fixture.CreateDbContext();
        var repository = new EventRepository(context);

        var events = await repository.GetAllAsync();
        var eventId = events.Items.First().Id;

        // Act
        var eventItem = await repository.GetByIdAsync(eventId);

        // Assert
        Assert.NotNull(eventItem);

        Assert.Equal(
            Microsoft.EntityFrameworkCore.EntityState.Detached,
            context.Entry(eventItem).State);
    }

}
