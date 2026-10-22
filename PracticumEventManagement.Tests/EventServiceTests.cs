using Microsoft.Extensions.DependencyInjection;
using PracticumEventManagement.Exceptions;
using PracticumEventManagement.Tests.Infrastructure;
using PracticumEventManagement.Models;
using PracticumEventManagement.Services;


namespace PracticumEventManagement.Tests;

public class EventServiceTests
{
    [Fact]
    public async Task Create_ShouldAddEventAndGenerateId()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var eventItem = new Event("Test event")
        {
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10
        };

        // Act
        var createdEvent = await service.CreateAsync(eventItem);

        // Assert
        Assert.NotEqual(Guid.Empty, createdEvent.Id);
        Assert.Equal("Test event", createdEvent.Title);
        Assert.Equal("Test description", createdEvent.Description);
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllEvents()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        await service.CreateAsync(new Event("Event 1")
        {
            Description = "Description 1",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10
        });

        await service.CreateAsync(new Event("Event 2")
        {
            Description = "Description 2",
            StartAt = DateTime.UtcNow.AddDays(3),
            EndAt = DateTime.UtcNow.AddDays(4),
            TotalSeats = 10
        });

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task GetById_ShouldReturnEvent_WhenEventExists()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var createdEvent = await service.CreateAsync(new Event("Test event")
        {
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10
        });

        // Act
        var result = await service.GetByIdAsync(createdEvent.Id);

        // Assert
        Assert.Equal(createdEvent.Id, result.Id);
        Assert.Equal(createdEvent.Title, result.Title);
        Assert.Equal(createdEvent.Description, result.Description);
    }

    [Fact]
    public async Task GetById_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();
        var id = Guid.NewGuid();

        // Act
        Func<Task> action = async () => await service.GetByIdAsync(id);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task Update_ShouldUpdateEvent_WhenEventExists()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var createdEvent = await service.CreateAsync(new Event("Old title")
        {
            Description = "Old description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10
        });

        var updatedEvent = new Event("New title")
        {
            Description = "New description",
            StartAt = DateTime.UtcNow.AddDays(3),
            EndAt = DateTime.UtcNow.AddDays(4),
            TotalSeats = 10
        };

        // Act
        var result = await service.UpdateAsync(createdEvent.Id, updatedEvent);
        var eventAfterUpdate = await service.GetByIdAsync(createdEvent.Id);

        // Assert
        Assert.True(result);
        Assert.Equal("New title", eventAfterUpdate.Title);
        Assert.Equal("New description", eventAfterUpdate.Description);
        Assert.Equal(updatedEvent.StartAt, eventAfterUpdate.StartAt);
        Assert.Equal(updatedEvent.EndAt, eventAfterUpdate.EndAt);
    }
    [Fact]
    public async Task Update_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var eventItem = new Event("Test event")
        {
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10
        };

        var id = Guid.NewGuid();

        // Act
        Func<Task> action = async () => await service.UpdateAsync(id, eventItem);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task Delete_ShouldDeleteEvent_WhenEventExists()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var createdEvent = await service.CreateAsync(new Event("Test event")
        {
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10
        });

        // Act
        var result = await service.DeleteAsync(createdEvent.Id);

        // Assert
        Assert.True(result);
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetByIdAsync(createdEvent.Id));
    }

    [Fact]
    public async Task Delete_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();
        var id = Guid.NewGuid();

        // Act
        Func<Task> action = async () => await service.DeleteAsync(id);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task GetAll_ShouldFilterByTitle_CaseInsensitive()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        await service.CreateAsync(new Event("Корпоратив новогодний")
        {
            Description = "Пьянка 1",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10
        });

        await service.CreateAsync(new Event("Тимбилдинг команды")
        {
            Description = "Пьянка 2",
            StartAt = DateTime.UtcNow.AddDays(3),
            EndAt = DateTime.UtcNow.AddDays(4),
            TotalSeats = 10
        });

        // Act
        var result = await service.GetAllAsync(title: "корпоратив");

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Корпоратив новогодний", result.Items.First().Title);
    }

    [Fact]
    public async Task GetAll_ShouldFilterByDates()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var baseDate = new DateTime(2026, 8, 1);

        await service.CreateAsync(new Event("Событие 1")
        {
            Description = "Описание 1",
            StartAt = baseDate.AddDays(1),
            EndAt = baseDate.AddDays(2),
            TotalSeats = 10
        });

        await service.CreateAsync(new Event("Событие 2")
        {
            Description = "Описание 2",
            StartAt = baseDate.AddDays(5),
            EndAt = baseDate.AddDays(6),
            TotalSeats = 10
        });

        await service.CreateAsync(new Event("Событие 3")
        {
            Description = "Описание 3",
            StartAt = baseDate.AddDays(10),
            EndAt = baseDate.AddDays(11),
            TotalSeats = 10
        });

        var from = baseDate.AddDays(4);
        var to = baseDate.AddDays(7);

        // Act
        var result = await service.GetAllAsync(from: from, to: to);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Событие 2", result.Items.First().Title);
    }

    [Fact]
    public async Task GetAll_ShouldApplyCombinedFilters()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var baseDate = new DateTime(2026, 8, 1);

        await service.CreateAsync(new Event("Корпоратив Новогодний")
        {
            Description = "Описание 1",
            StartAt = baseDate.AddDays(5),
            EndAt = baseDate.AddDays(6),
            TotalSeats = 10
        });

        await service.CreateAsync(new Event("Корпоратив летний")
        {
            Description = "Описание 2",
            StartAt = baseDate.AddDays(10),
            EndAt = baseDate.AddDays(11),
            TotalSeats = 10
        });

        await service.CreateAsync(new Event("День рождение компании")
        {
            Description = "Описание 3",
            StartAt = baseDate.AddDays(5),
            EndAt = baseDate.AddDays(6),
            TotalSeats = 10
        });

        var from = baseDate.AddDays(4);
        var to = baseDate.AddDays(7);

        // Act
        var result = await service.GetAllAsync(
            title: "корпоратив",
            from: from,
            to: to);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Корпоратив Новогодний", result.Items.First().Title);
    }


    [Fact]
    public async Task GetAll_ShouldReturnCorrectPage()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var baseDate = new DateTime(2026, 8, 1);

        for (var i = 1; i <= 5; i++)
        {
            await service.CreateAsync(new Event($"Событие {i}")
            {
                Description = $"Описание {i}",
                StartAt = baseDate.AddDays(i),
                EndAt = baseDate.AddDays(i + 1),
                TotalSeats = 10
            });
        }

        // Act
        var result = await service.GetAllAsync(page: 2, pageSize: 2);

        // Assert
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.Count);

        var items = result.Items.ToList();

        Assert.Equal(2, items.Count);
        Assert.Equal("Событие 3", items[0].Title);
        Assert.Equal("Событие 4", items[1].Title);
    }
    [Fact]
    public async Task Create_ShouldThrowValidationException_WhenEndAtIsBeforeStartAt()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var eventItem = new Event("Invalid event")
        {
            Description = "Invalid dates",
            StartAt = new DateTime(2026, 8, 10),
            EndAt = new DateTime(2026, 8, 9)
        };

        // Act
        Func<Task> action = async () => await service.CreateAsync(eventItem);

        // Assert
        await Assert.ThrowsAsync<ValidationException>(action);
    }
    [Fact]
    public async Task Update_ShouldThrowValidationException_WhenEndAtIsBeforeStartAt()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var createdEvent = await service.CreateAsync(new Event("Test event")
        {
            Description = "Valid dates",
            StartAt = new DateTime(2026, 8, 10),
            EndAt = new DateTime(2026, 8, 11),
            TotalSeats = 10
        });

        var invalidEvent = new Event("Updated event")
        {
            Description = "Invalid dates",
            StartAt = new DateTime(2026, 8, 15),
            EndAt = new DateTime(2026, 8, 14),
            TotalSeats = 10
        };

        // Act
        Func<Task> action = async () => await service.UpdateAsync(createdEvent.Id, invalidEvent);

        // Assert
        await Assert.ThrowsAsync<ValidationException>(action);
    }

    [Fact]
    public async Task GetAll_ShouldThrowValidationException_WhenPageIsLessThanOne()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        // Act
        Func<Task> action = async () => await service.GetAllAsync(page: 0);

        // Assert
        await Assert.ThrowsAsync<ValidationException>(action);
    }

    [Fact]
    public async Task GetAll_ShouldThrowValidationException_WhenPageSizeIsLessThanOne()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        // Act
        Func<Task> action = async () => await service.GetAllAsync(pageSize: 0);

        // Assert
        await Assert.ThrowsAsync<ValidationException>(action);
    }

    [Fact]
    public async Task Create_ShouldSetAvailableSeatsEqualToTotalSeats()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var eventItem = new Event("Test event")
        {
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10
        };

        // Act
        var createdEvent = await service.CreateAsync(eventItem);

        // Assert
        Assert.Equal(10, createdEvent.TotalSeats);
        Assert.Equal(10, createdEvent.AvailableSeats);
    }

    [Fact]
    public async Task Create_ShouldThrowValidationException_WhenTotalSeatsIsNotPositive()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var eventItem = new Event("Test event")
        {
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 0
        };

        // Act
        Func<Task> action = async () => await service.CreateAsync(eventItem);

        // Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(action);

        Assert.Equal(
            "TotalSeats must be greater than zero.",
            exception.Message);
    }
}