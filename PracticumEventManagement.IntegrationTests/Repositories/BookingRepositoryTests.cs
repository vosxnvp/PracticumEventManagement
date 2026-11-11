
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PracticumEventManagement.DataAccess.Repositories;
using PracticumEventManagement.IntegrationTests.Fixtures;
using PracticumEventManagement.Models;
using Xunit;

namespace PracticumEventManagement.IntegrationTests.Repositories;

[Collection("PostgreSQL")]
public class BookingRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public BookingRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> CreateEventAsync()
    {
        await using var context = _fixture.CreateDbContext();

        var repository = new EventRepository(context);

        var eventItem = new Event("Тестовое мероприятие")
        {
            Id = Guid.NewGuid(),
            StartAt = new DateTime(
                2027, 5, 10, 10, 0, 0, DateTimeKind.Utc),
            EndAt = new DateTime(
                2027, 5, 10, 18, 0, 0, DateTimeKind.Utc),
            TotalSeats = 100,
            AvailableSeats = 100
        };

        await repository.AddAsync(eventItem);
        await repository.SaveChangesAsync();

        return eventItem.Id;
    }

    [Fact]
    public async Task AddAsync_ShouldPersistBooking()
    {
        // Arrange
        var eventId = await CreateEventAsync();

        var booking = new Booking(eventId)
        {
            Id = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await using (var context = _fixture.CreateDbContext())
        {
            var repository = new BookingRepository(context);

            await repository.AddAsync(booking);
            await repository.SaveChangesAsync();
        }

        // Assert
        await using (var context = _fixture.CreateDbContext())
        {
            var repository = new BookingRepository(context);

            var savedBooking =
                await repository.GetByIdAsync(booking.Id);

            Assert.NotNull(savedBooking);
            Assert.Equal(eventId, savedBooking.EventId);
            Assert.Equal(BookingStatus.Pending, savedBooking.Status);
            Assert.Null(savedBooking.ProcessedAt);
        }
    }

    [Fact]
    public async Task GetTrackedByIdAsync_ShouldAllowUpdatingBooking()
    {
        // Arrange
        var eventId = await CreateEventAsync();

        var booking = new Booking(eventId)
        {
            Id = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await using (var context = _fixture.CreateDbContext())
        {
            var repository = new BookingRepository(context);
            await repository.AddAsync(booking);
            await repository.SaveChangesAsync();
        }

        // Act
        await using (var context = _fixture.CreateDbContext())
        {
            var repository = new BookingRepository(context);

            var trackedBooking =
                await repository.GetTrackedByIdAsync(booking.Id);

            Assert.NotNull(trackedBooking);

            trackedBooking.Confirm();

            await repository.SaveChangesAsync();
        }

        // Assert
        await using (var context = _fixture.CreateDbContext())
        {
            var repository = new BookingRepository(context);

            var updatedBooking =
                await repository.GetByIdAsync(booking.Id);

            Assert.NotNull(updatedBooking);
            Assert.Equal(
                BookingStatus.Confirmed,
                updatedBooking.Status);

            Assert.NotNull(updatedBooking.ProcessedAt);
        }
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenBookingDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();

        var repository = new BookingRepository(context);

        // Act
        var result =
            await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPendingAsync_ShouldReturnOnlyPendingBookings()
    {
        // Arrange
        var eventId = await CreateEventAsync();

        var pendingBooking = new Booking(eventId)
        {
            Id = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-2)
        };

        var confirmedBooking = new Booking(eventId)
        {
            Id = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        confirmedBooking.Confirm();

        await using (var context = _fixture.CreateDbContext())
        {
            var repository = new BookingRepository(context);

            await repository.AddAsync(pendingBooking);
            await repository.AddAsync(confirmedBooking);
            await repository.SaveChangesAsync();
        }

        // Act
        await using var readContext = _fixture.CreateDbContext();
        var readRepository = new BookingRepository(readContext);

        var result = await readRepository.GetPendingAsync();

        // Assert
        var booking = Assert.Single(result);

        Assert.Equal(pendingBooking.Id, booking.Id);
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [Fact]
    public async Task GetPendingIdsAsync_ShouldReturnPendingIdsInOrder()
    {
        // Arrange
        var eventId = await CreateEventAsync();

        var firstBooking = new Booking(eventId)
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddMinutes(-3)
        };

        var secondBooking = new Booking(eventId)
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddMinutes(-2)
        };

        var rejectedBooking = new Booking(eventId)
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        rejectedBooking.Reject();

        await using (var context = _fixture.CreateDbContext())
        {
            var repository = new BookingRepository(context);

            await repository.AddAsync(secondBooking);
            await repository.AddAsync(rejectedBooking);
            await repository.AddAsync(firstBooking);

            await repository.SaveChangesAsync();
        }

        // Act
        await using var readContext = _fixture.CreateDbContext();
        var readRepository = new BookingRepository(readContext);

        var ids = await readRepository.GetPendingIdsAsync();

        // Assert
        Assert.Equal(
            new[] { firstBooking.Id, secondBooking.Id },
            ids);
    }

    [Fact]
    public async Task Remove_ShouldDeleteBooking()
    {
        // Arrange
        var eventId = await CreateEventAsync();

        var booking = new Booking(eventId)
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        await using (var context = _fixture.CreateDbContext())
        {
            var repository = new BookingRepository(context);

            await repository.AddAsync(booking);
            await repository.SaveChangesAsync();
        }

        // Act
        await using (var context = _fixture.CreateDbContext())
        {
            var repository = new BookingRepository(context);

            var trackedBooking =
                await repository.GetTrackedByIdAsync(booking.Id);

            Assert.NotNull(trackedBooking);

            repository.Remove(trackedBooking);
            await repository.SaveChangesAsync();
        }

        // Assert
        await using var readContext = _fixture.CreateDbContext();
        var readRepository = new BookingRepository(readContext);

        var deletedBooking =
            await readRepository.GetByIdAsync(booking.Id);

        Assert.Null(deletedBooking);
    }

    [Fact]
    public async Task GetTrackedByIdAsync_ShouldReturnNull_WhenBookingDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new BookingRepository(context);

        // Act
        var result = await repository.GetTrackedByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_ShouldFail_WhenEventDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new BookingRepository(context);

        var booking = new Booking(Guid.NewGuid())
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(booking);

        // Act
        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => repository.SaveChangesAsync());

        // Assert
        var postgresException =
            Assert.IsType<PostgresException>(exception.InnerException);

        Assert.Equal(
            PostgresErrorCodes.ForeignKeyViolation,
            postgresException.SqlState);
    }

    [Fact]
    public async Task DeleteEvent_ShouldCascadeDeleteBookings()
    {
        // Arrange
        var eventId = await CreateEventAsync();

        var firstBooking = new Booking(eventId)
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        var secondBooking = new Booking(eventId)
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddMinutes(1)
        };

        await using (var context = _fixture.CreateDbContext())
        {
            var bookingRepository = new BookingRepository(context);

            await bookingRepository.AddAsync(firstBooking);
            await bookingRepository.AddAsync(secondBooking);
            await bookingRepository.SaveChangesAsync();
        }

        // Act
        await using (var context = _fixture.CreateDbContext())
        {
            var eventRepository = new EventRepository(context);

            var eventItem =
                await eventRepository.GetTrackedByIdAsync(eventId);

            Assert.NotNull(eventItem);

            eventRepository.Remove(eventItem);
            await eventRepository.SaveChangesAsync();
        }

        // Assert
        await using var readContext = _fixture.CreateDbContext();

        var bookingRepositoryForRead =
            new BookingRepository(readContext);

        Assert.Null(
            await bookingRepositoryForRead.GetByIdAsync(firstBooking.Id));

        Assert.Null(
            await bookingRepositoryForRead.GetByIdAsync(secondBooking.Id));
    }

}
