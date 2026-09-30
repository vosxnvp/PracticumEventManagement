using PracticumEventManagement.Exceptions;
using PracticumEventManagement.Models;
using PracticumEventManagement.Services;

namespace PracticumEventManagement.Tests;

public class BookingServiceTests
{

    [Fact]
    public async Task CreateBookingAsync_ShouldCreatePendingBooking_WhenEventExists()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = eventService.Create(new Event
        {
            Title = "Test event",
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
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = eventService.Create(new Event
        {
            Title = "Test event",
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
    private static Event CreateTestEvent(
    EventService eventService,
    int totalSeats = 10)
    {
        return eventService.Create(new Event
        {
            Title = "Test event",
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
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = eventService.Create(new Event
        {
            Title = "Test event",
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
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = eventService.Create(new Event
        {
            Title = "Test event",
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
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = eventService.Create(new Event
        {
            Title = "Test event",
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
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

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
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = eventService.Create(new Event
        {
            Title = "Deleted event",
            Description = "Test description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10
        });

        eventService.Delete(eventItem.Id);

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
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

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
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

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
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

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
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = eventService.Create(new Event
        {
            Title = "Test event",
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
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = CreateTestEvent(eventService, totalSeats: 3);

        // Act
        await bookingService.CreateBookingAsync(eventItem.Id);

        // Assert
        Assert.Equal(2, eventItem.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldCreateBookingsUpToSeatLimit()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = CreateTestEvent(eventService, totalSeats: 3);

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
        Assert.Equal(0, eventItem.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNoAvailableSeatsException_WhenNoSeatsLeft()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = CreateTestEvent(eventService, totalSeats: 1);

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

        Assert.Equal(0, eventItem.AvailableSeats);
    }
    [Fact]
    public async Task RejectAndReleaseSeats_ShouldRestoreAvailableSeats()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = CreateTestEvent(eventService, totalSeats: 1);

        var booking =
            await bookingService.CreateBookingAsync(eventItem.Id);

        Assert.Equal(0, eventItem.AvailableSeats);

        // Act
        await bookingService.RejectBookingAsync(booking.Id);
        eventItem.ReleaseSeats();

        // Assert
        Assert.Equal(1, eventItem.AvailableSeats);
    }
    [Fact]
    public async Task RejectAndReleaseSeats_ShouldAllowNewBooking()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = CreateTestEvent(eventService, totalSeats: 1);

        var firstBooking =
            await bookingService.CreateBookingAsync(eventItem.Id);

        await bookingService.RejectBookingAsync(firstBooking.Id);
        eventItem.ReleaseSeats();

        // Act
        var secondBooking =
            await bookingService.CreateBookingAsync(eventItem.Id);

        // Assert
        Assert.NotEqual(firstBooking.Id, secondBooking.Id);
        Assert.Equal(BookingStatus.Pending, secondBooking.Status);
        Assert.Equal(0, eventItem.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldPreventOverbooking_WhenRequestsAreConcurrent()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = CreateTestEvent(eventService, totalSeats: 5);

        // Act
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    await bookingService.CreateBookingAsync(eventItem.Id);
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
        Assert.Equal(0, eventItem.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldCreateUniqueIds_WhenRequestsAreConcurrent()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = CreateTestEvent(eventService, totalSeats: 10);

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(async () =>
                await bookingService.CreateBookingAsync(eventItem.Id)))
            .ToArray();

        var bookings = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, bookings.Length);
        Assert.Equal(
            10,
            bookings.Select(booking => booking.Id)
                .Distinct()
                .Count());

        Assert.Equal(0, eventItem.AvailableSeats);
    }
}