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
            EndAt = DateTime.UtcNow.AddDays(2)
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
            EndAt = DateTime.UtcNow.AddDays(2)
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
            EndAt = DateTime.UtcNow.AddDays(2)
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
            EndAt = DateTime.UtcNow.AddDays(2)
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
            EndAt = DateTime.UtcNow.AddDays(2)
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
            EndAt = DateTime.UtcNow.AddDays(2)
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
            EndAt = DateTime.UtcNow.AddDays(2)
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
}