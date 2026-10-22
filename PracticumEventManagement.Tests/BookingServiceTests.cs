using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PracticumEventManagement.DataAccess;
using PracticumEventManagement.Exceptions;
using PracticumEventManagement.Tests.Infrastructure;
using PracticumEventManagement.Models;
using PracticumEventManagement.Services;

namespace PracticumEventManagement.Tests;

public class BookingServiceTests
{

    [Fact]
    public async Task CreateBookingAsync_ShouldCreatePendingBooking_WhenEventExists()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var eventItem = await eventService.CreateAsync(new Event("Test event")
        {
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10
        });

        // Act
        var booking = await bookingService.CreateBookingAsync(eventItem.Id);

        // Assert
        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(eventItem.Id, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.NotEqual(default, booking.CreatedAt);
        Assert.Null(booking.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldCreateUniqueIds_WhenCreatingMultipleBookings()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var eventItem = await eventService.CreateAsync(new Event("Test event")
        {
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10
        });

        // Act
        var firstBooking =
            await bookingService.CreateBookingAsync(eventItem.Id);

        var secondBooking =
            await bookingService.CreateBookingAsync(eventItem.Id);

        // Assert
        Assert.NotEqual(firstBooking.Id, secondBooking.Id);
        Assert.Equal(eventItem.Id, firstBooking.EventId);
        Assert.Equal(eventItem.Id, secondBooking.EventId);
    }
    private static async Task<Event> CreateTestEventAsync(
        IEventService eventService,
        int totalSeats = 10)
    {
        return await eventService.CreateAsync(new Event("Test event")
        {
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = totalSeats
        });
    }
    [Fact]
    public async Task GetBookingByIdAsync_ShouldReturnBooking_WhenBookingExists()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var eventItem = await eventService.CreateAsync(new Event("Test event")
        {
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10
        });

        var createdBooking =
            await bookingService.CreateBookingAsync(eventItem.Id);

        // Act
        var booking =
            await bookingService.GetBookingByIdAsync(createdBooking.Id);

        // Assert
        Assert.Equal(createdBooking.Id, booking.Id);
        Assert.Equal(createdBooking.EventId, booking.EventId);
        Assert.Equal(createdBooking.Status, booking.Status);
        Assert.Equal(createdBooking.CreatedAt, booking.CreatedAt);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldReflectConfirmedStatus()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var eventItem = await eventService.CreateAsync(new Event("Test event")
        {
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10
        });

        var createdBooking =
            await bookingService.CreateBookingAsync(eventItem.Id);

        // Act
        await bookingService.ConfirmBookingAsync(createdBooking.Id);

        var booking =
            await bookingService.GetBookingByIdAsync(createdBooking.Id);

        // Assert
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldReflectRejectedStatus()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var eventItem = await eventService.CreateAsync(new Event("Test event")
        {
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10
        });

        var createdBooking =
            await bookingService.CreateBookingAsync(eventItem.Id);

        // Act
        await bookingService.RejectBookingAsync(createdBooking.Id);

        var booking =
            await bookingService.GetBookingByIdAsync(createdBooking.Id);

        // Assert
        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var eventId = Guid.NewGuid();

        // Act
        var action = () => bookingService.CreateBookingAsync(eventId);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNotFoundException_WhenEventWasDeleted()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var eventItem = await eventService.CreateAsync(new Event("Deleted event")
        {
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10
        });

        await eventService.DeleteAsync(eventItem.Id);

        // Act
        var action = () =>
            bookingService.CreateBookingAsync(eventItem.Id);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldThrowNotFoundException_WhenBookingDoesNotExist()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var bookingId = Guid.NewGuid();

        // Act
        var action = () =>
            bookingService.GetBookingByIdAsync(bookingId);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task ConfirmBookingAsync_ShouldThrowNotFoundException_WhenBookingDoesNotExist()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var bookingId = Guid.NewGuid();

        // Act
        var action = () =>
            bookingService.ConfirmBookingAsync(bookingId);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task RejectBookingAsync_ShouldThrowNotFoundException_WhenBookingDoesNotExist()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var bookingId = Guid.NewGuid();

        // Act
        var action = () =>
            bookingService.RejectBookingAsync(bookingId);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task GetPendingBookingsAsync_ShouldReturnOnlyPendingBookings()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var eventItem = await eventService.CreateAsync(new Event("Test event")
        {
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10
        });

        var firstBooking =
            await bookingService.CreateBookingAsync(eventItem.Id);

        var secondBooking =
            await bookingService.CreateBookingAsync(eventItem.Id);

        await bookingService.ConfirmBookingAsync(firstBooking.Id);

        // Act
        var pendingBookings =
            await bookingService.GetPendingBookingsAsync();

        // Assert
        Assert.DoesNotContain(
            pendingBookings,
            booking => booking.Id == firstBooking.Id);

        Assert.Contains(
            pendingBookings,
            booking => booking.Id == secondBooking.Id);

        Assert.All(
            pendingBookings,
            booking => Assert.Equal(BookingStatus.Pending, booking.Status));
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldDecreaseAvailableSeats()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var eventItem = await CreateTestEventAsync(eventService, totalSeats: 3);

        // Act
        await bookingService.CreateBookingAsync(eventItem.Id);

        // Assert
        Assert.Equal(2, await GetAvailableSeatsAsync(provider, eventItem.Id));
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldCreateBookingsUpToSeatLimit()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var eventItem = await CreateTestEventAsync(eventService, totalSeats: 3);

        // Act
        var firstBooking =
            await bookingService.CreateBookingAsync(eventItem.Id);

        var secondBooking =
            await bookingService.CreateBookingAsync(eventItem.Id);

        var thirdBooking =
            await bookingService.CreateBookingAsync(eventItem.Id);

        // Assert
        var bookingIds = new[]
        {
        firstBooking.Id,
        secondBooking.Id,
        thirdBooking.Id
    };

        Assert.Equal(3, bookingIds.Distinct().Count());
        Assert.Equal(0, await GetAvailableSeatsAsync(provider, eventItem.Id));
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNoAvailableSeatsException_WhenNoSeatsLeft()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var eventItem = await CreateTestEventAsync(eventService, totalSeats: 1);

        await bookingService.CreateBookingAsync(eventItem.Id);

        // Act
        var action = () =>
            bookingService.CreateBookingAsync(eventItem.Id);

        // Assert
        var exception =
            await Assert.ThrowsAsync<NoAvailableSeatsException>(action);

        Assert.Equal(
            "No available seats for this event",
            exception.Message);

        Assert.Equal(0, await GetAvailableSeatsAsync(provider, eventItem.Id));
    }
    [Fact]
    public async Task RejectAndReleaseSeats_ShouldRestoreAvailableSeats()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var eventItem = await CreateTestEventAsync(eventService, totalSeats: 1);

        var booking =
            await bookingService.CreateBookingAsync(eventItem.Id);

        Assert.Equal(0, await GetAvailableSeatsAsync(provider, eventItem.Id));

        // Act
        await bookingService.RejectBookingAsync(booking.Id);
        await ReleaseSeatsAsync(provider, eventItem.Id);

        // Assert
        Assert.Equal(1, await GetAvailableSeatsAsync(provider, eventItem.Id));
    }
    [Fact]
    public async Task RejectAndReleaseSeats_ShouldAllowNewBooking()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var eventItem = await CreateTestEventAsync(eventService, totalSeats: 1);

        var firstBooking =
            await bookingService.CreateBookingAsync(eventItem.Id);

        await bookingService.RejectBookingAsync(firstBooking.Id);
        await ReleaseSeatsAsync(provider, eventItem.Id);

        // Act: the first scope has a tracked copy with the old seat count.
        // Use a fresh scope after changing the event in another DbContext.
        using var newBookingScope = provider.CreateScope();
        var newBookingService = newBookingScope.ServiceProvider
            .GetRequiredService<IBookingService>();
        var secondBooking =
            await newBookingService.CreateBookingAsync(eventItem.Id);

        // Assert
        Assert.NotEqual(firstBooking.Id, secondBooking.Id);
        Assert.Equal(BookingStatus.Pending, secondBooking.Status);
        Assert.Equal(0, await GetAvailableSeatsAsync(provider, eventItem.Id));
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldPreventOverbooking_WhenRequestsAreConcurrent()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var eventItem = await CreateTestEventAsync(eventService, totalSeats: 5);

        // Act
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    using var requestScope = provider.CreateScope();
                    var requestService = requestScope.ServiceProvider.GetRequiredService<IBookingService>();
                    await requestService.CreateBookingAsync(eventItem.Id);
                    return true;
                }
                catch (NoAvailableSeatsException)
                {
                    return false;
                }
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(5, results.Count(success => success));
        Assert.Equal(15, results.Count(success => !success));
        Assert.Equal(0, await GetAvailableSeatsAsync(provider, eventItem.Id));
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldCreateUniqueIds_WhenRequestsAreConcurrent()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();
        using var scope = provider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var eventItem = await CreateTestEventAsync(eventService, totalSeats: 10);

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(async () =>
            {
                using var requestScope = provider.CreateScope();
                var requestService = requestScope.ServiceProvider.GetRequiredService<IBookingService>();
                return await requestService.CreateBookingAsync(eventItem.Id);
            }))
            .ToArray();

        var bookings = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, bookings.Length);
        Assert.Equal(
            10,
            bookings.Select(booking => booking.Id)
                .Distinct()
                .Count());

        Assert.Equal(0, await GetAvailableSeatsAsync(provider, eventItem.Id));
    }

    private static async Task<int> GetAvailableSeatsAsync(
        IServiceProvider provider, Guid eventId)
    {
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();
        var eventItem = await service.GetByIdAsync(eventId);
        return eventItem.AvailableSeats;
    }

    private static async Task ReleaseSeatsAsync(
        IServiceProvider provider, Guid eventId)
    {
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eventItem = await context.Events.SingleAsync(e => e.Id == eventId);
        eventItem.ReleaseSeats();
        await context.SaveChangesAsync();
    }
}
