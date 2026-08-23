using PracticumEventManagement.Exceptions;
using PracticumEventManagement.Models;
using PracticumEventManagement.Services;


namespace PracticumEventManagement.Tests;

public class EventServiceTests
{
    [Fact]
    public void Create_ShouldAddEventAndGenerateId()
    {
        // Arrange
        var service = new EventService();

        var eventItem = new Event
        {
            Title = "Test event",
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        };

        // Act
        var createdEvent = service.Create(eventItem);

        // Assert
        Assert.NotEqual(Guid.Empty, createdEvent.Id);
        Assert.Equal("Test event", createdEvent.Title);
        Assert.Equal("Test description", createdEvent.Description);
    }

    [Fact]
    public void GetAll_ShouldReturnAllEvents()
    {
        // Arrange
        var service = new EventService();

        service.Create(new Event
        {
            Title = "Event 1",
            Description = "Description 1",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        });

        service.Create(new Event
        {
            Title = "Event 2",
            Description = "Description 2",
            StartAt = DateTime.UtcNow.AddDays(3),
            EndAt = DateTime.UtcNow.AddDays(4)
        });

        // Act
        var result = service.GetAll();

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public void GetById_ShouldReturnEvent_WhenEventExists()
    {
        // Arrange
        var service = new EventService();

        var createdEvent = service.Create(new Event
        {
            Title = "Test event",
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        });

        // Act
        var result = service.GetById(createdEvent.Id);

        // Assert
        Assert.Equal(createdEvent.Id, result.Id);
        Assert.Equal(createdEvent.Title, result.Title);
        Assert.Equal(createdEvent.Description, result.Description);
    }

    [Fact]
    public void GetById_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        var service = new EventService();
        var id = Guid.NewGuid();

        // Act
        var action = () => service.GetById(id);

        // Assert
        Assert.Throws<NotFoundException>(action);
    }

    [Fact]
    public void Update_ShouldUpdateEvent_WhenEventExists()
    {
        // Arrange
        var service = new EventService();

        var createdEvent = service.Create(new Event
        {
            Title = "Old title",
            Description = "Old description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        });

        var updatedEvent = new Event
        {
            Title = "New title",
            Description = "New description",
            StartAt = DateTime.UtcNow.AddDays(3),
            EndAt = DateTime.UtcNow.AddDays(4)
        };

        // Act
        var result = service.Update(createdEvent.Id, updatedEvent);
        var eventAfterUpdate = service.GetById(createdEvent.Id);

        // Assert
        Assert.True(result);
        Assert.Equal("New title", eventAfterUpdate.Title);
        Assert.Equal("New description", eventAfterUpdate.Description);
        Assert.Equal(updatedEvent.StartAt, eventAfterUpdate.StartAt);
        Assert.Equal(updatedEvent.EndAt, eventAfterUpdate.EndAt);
    }
    [Fact]
    public void Update_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        var service = new EventService();

        var eventItem = new Event
        {
            Title = "Test event",
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        };

        var id = Guid.NewGuid();

        // Act
        Action action = () => service.Update(id, eventItem);

        // Assert
        Assert.Throws<NotFoundException>(action);
    }

    [Fact]
    public void Delete_ShouldDeleteEvent_WhenEventExists()
    {
        // Arrange
        var service = new EventService();

        var createdEvent = service.Create(new Event
        {
            Title = "Test event",
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        });

        // Act
        var result = service.Delete(createdEvent.Id);

        // Assert
        Assert.True(result);
        Assert.Throws<NotFoundException>(
            () => service.GetById(createdEvent.Id));
    }

    [Fact]
    public void Delete_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        var service = new EventService();
        var id = Guid.NewGuid();

        // Act
        Action action = () => service.Delete(id);

        // Assert
        Assert.Throws<NotFoundException>(action);
    }

    [Fact]
    public void GetAll_ShouldFilterByTitle_CaseInsensitive()
    {
        // Arrange
        var service = new EventService();

        service.Create(new Event
        {
            Title = "Корпоратив новогодний",
            Description = "Пьянка 1",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        });

        service.Create(new Event
        {
            Title = "Тимбилдинг команды",
            Description = "Пьянка 2",
            StartAt = DateTime.UtcNow.AddDays(3),
            EndAt = DateTime.UtcNow.AddDays(4)
        });

        // Act
        var result = service.GetAll(title: "корпоратив");

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Корпоратив новогодний", result.Items.First().Title);
    }

    [Fact]
    public void GetAll_ShouldFilterByDates()
    {
        // Arrange
        var service = new EventService();

        var baseDate = new DateTime(2026, 8, 1);

        service.Create(new Event
        {
            Title = "Событие 1",
            Description = "Описание 1",
            StartAt = baseDate.AddDays(1),
            EndAt = baseDate.AddDays(2)
        });

        service.Create(new Event
        {
            Title = "Событие 2",
            Description = "Описание 2",
            StartAt = baseDate.AddDays(5),
            EndAt = baseDate.AddDays(6)
        });

        service.Create(new Event
        {
            Title = "Событие 3",
            Description = "Описание 3",
            StartAt = baseDate.AddDays(10),
            EndAt = baseDate.AddDays(11)
        });

        var from = baseDate.AddDays(4);
        var to = baseDate.AddDays(7);

        // Act
        var result = service.GetAll(from: from, to: to);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Событие 2", result.Items.First().Title);
    }

    [Fact]
    public void GetAll_ShouldApplyCombinedFilters()
    {
        // Arrange
        var service = new EventService();

        var baseDate = new DateTime(2026, 8, 1);

        service.Create(new Event
        {
            Title = "Корпоратив Новогодний",
            Description = "Описание 1",
            StartAt = baseDate.AddDays(5),
            EndAt = baseDate.AddDays(6)
        });

        service.Create(new Event
        {
            Title = "Корпоратив летний",
            Description = "Описание 2",
            StartAt = baseDate.AddDays(10),
            EndAt = baseDate.AddDays(11)
        });

        service.Create(new Event
        {
            Title = "День рождение компании",
            Description = "Описание 3",
            StartAt = baseDate.AddDays(5),
            EndAt = baseDate.AddDays(6)
        });

        var from = baseDate.AddDays(4);
        var to = baseDate.AddDays(7);

        // Act
        var result = service.GetAll(
            title: "корпоратив",
            from: from,
            to: to);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Корпоратив Новогодний", result.Items.First().Title);
    }


    [Fact]
    public void GetAll_ShouldReturnCorrectPage()
    {
        // Arrange
        var service = new EventService();

        var baseDate = new DateTime(2026, 8, 1);

        for (var i = 1; i <= 5; i++)
        {
            service.Create(new Event
            {
                Title = $"Событие {i}",
                Description = $"Описание {i}",
                StartAt = baseDate.AddDays(i),
                EndAt = baseDate.AddDays(i + 1)
            });
        }

        // Act
        var result = service.GetAll(page: 2, pageSize: 2);

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
    public void Create_ShouldThrowValidationException_WhenEndAtIsBeforeStartAt()
    {
        // Arrange
        var service = new EventService();

        var eventItem = new Event
        {
            Title = "Invalid event",
            Description = "Invalid dates",
            StartAt = new DateTime(2026, 8, 10),
            EndAt = new DateTime(2026, 8, 9)
        };

        // Act
        Action action = () => service.Create(eventItem);

        // Assert
        Assert.Throws<ValidationException>(action);
    }
    [Fact]
    public void Update_ShouldThrowValidationException_WhenEndAtIsBeforeStartAt()
    {
        // Arrange
        var service = new EventService();

        var createdEvent = service.Create(new Event
        {
            Title = "Test event",
            Description = "Valid dates",
            StartAt = new DateTime(2026, 8, 10),
            EndAt = new DateTime(2026, 8, 11)
        });

        var invalidEvent = new Event
        {
            Title = "Updated event",
            Description = "Invalid dates",
            StartAt = new DateTime(2026, 8, 15),
            EndAt = new DateTime(2026, 8, 14)
        };

        // Act
        Action action = () => service.Update(createdEvent.Id, invalidEvent);

        // Assert
        Assert.Throws<ValidationException>(action);
    }

    [Fact]
    public void GetAll_ShouldThrowValidationException_WhenPageIsLessThanOne()
    {
        // Arrange
        var service = new EventService();

        // Act
        Action action = () => service.GetAll(page: 0);

        // Assert
        Assert.Throws<ValidationException>(action);
    }

    [Fact]
    public void GetAll_ShouldThrowValidationException_WhenPageSizeIsLessThanOne()
    {
        // Arrange
        var service = new EventService();

        // Act
        Action action = () => service.GetAll(pageSize: 0);

        // Assert
        Assert.Throws<ValidationException>(action);
    }
}