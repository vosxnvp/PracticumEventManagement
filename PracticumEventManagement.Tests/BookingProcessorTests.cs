using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PracticumEventManagement.Application.Interfaces;
using PracticumEventManagement.Application.Services;
using PracticumEventManagement.Domain.Entities;
using PracticumEventManagement.Domain.Enums;
using PracticumEventManagement.Infrastructure.DataAccess;
using PracticumEventManagement.Tests.Infrastructure;

namespace PracticumEventManagement.Tests;

public class BookingProcessorTests
{
    [Fact]
    public async Task ProcessAsync_ShouldConfirmBooking_WhenEventExists()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();

        var (eventId, bookingId) =
            await CreatePendingBookingAsync(provider);

        using var scope = provider.CreateScope();

        var processor = scope.ServiceProvider
            .GetRequiredService<IBookingProcessor>();

        // Act
        var result = await processor.ProcessAsync(
            bookingId,
            CancellationToken.None);

        // Assert
        Assert.True(result.HasValue);
        Assert.Equal(BookingStatus.Confirmed, result.Value);

        using var checkScope = provider.CreateScope();

        var bookingService = checkScope.ServiceProvider
            .GetRequiredService<IBookingService>();

        var eventService = checkScope.ServiceProvider
            .GetRequiredService<IEventService>();

        var booking = await bookingService
            .GetBookingByIdAsync(bookingId);

        var eventItem = await eventService
            .GetByIdAsync(eventId);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.Equal(0, eventItem.AvailableSeats);
    }

    [Fact]
    public async Task ProcessAsync_ShouldRejectBooking_WhenEventNotFound()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();

        var (_, bookingId) =
            await CreatePendingBookingAsync(provider);

        // EF InMemory не проверяет внешние ключи так,
        // как PostgreSQL. Искусственно создаём ссылку
        // на отсутствующее событие, чтобы проверить эту ветку.
        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var booking = await context.Set<Booking>()
                .SingleAsync(b => b.Id == bookingId);

            context.Entry(booking)
                .Property(nameof(Booking.EventId))
                .CurrentValue = Guid.NewGuid();

            await context.SaveChangesAsync();
        }

        using var processingScope = provider.CreateScope();

        var processor = processingScope.ServiceProvider
            .GetRequiredService<IBookingProcessor>();

        // Act
        var result = await processor.ProcessAsync(
            bookingId,
            CancellationToken.None);

        // Assert
        Assert.True(result.HasValue);
        Assert.Equal(BookingStatus.Rejected, result.Value);

        using var checkScope = provider.CreateScope();

        var bookingService = checkScope.ServiceProvider
            .GetRequiredService<IBookingService>();

        var bookingInfo = await bookingService
            .GetBookingByIdAsync(bookingId);

        Assert.Equal(BookingStatus.Rejected, bookingInfo.Status);
        Assert.NotNull(bookingInfo.ProcessedAt);
    }

    [Fact]
    public async Task ProcessAsync_ShouldSkipAlreadyProcessedBooking()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();

        var (_, bookingId) =
            await CreatePendingBookingAsync(provider);

        using var scope = provider.CreateScope();

        var processor = scope.ServiceProvider
            .GetRequiredService<IBookingProcessor>();

        // Сначала подтверждаем бронь.
        await processor.ProcessAsync(
            bookingId,
            CancellationToken.None);

        // Act: пытаемся обработать её повторно.
        var result = await processor.ProcessAsync(
            bookingId,
            CancellationToken.None);

        // Assert
        Assert.Null(result);

        using var checkScope = provider.CreateScope();

        var bookingService = checkScope.ServiceProvider
            .GetRequiredService<IBookingService>();

        var bookingInfo = await bookingService
            .GetBookingByIdAsync(bookingId);

        Assert.Equal(BookingStatus.Confirmed, bookingInfo.Status);
    }

    [Fact]
    public async Task RejectAfterErrorAsync_ShouldReleaseSeatAndRejectBooking()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();

        var (eventId, bookingId) =
            await CreatePendingBookingAsync(provider);

        using var scope = provider.CreateScope();

        var processor = scope.ServiceProvider
            .GetRequiredService<IBookingProcessor>();

        // Act
        await processor.RejectAfterErrorAsync(
            bookingId,
            CancellationToken.None);

        // Assert
        using var checkScope = provider.CreateScope();

        var bookingService = checkScope.ServiceProvider
            .GetRequiredService<IBookingService>();

        var eventService = checkScope.ServiceProvider
            .GetRequiredService<IEventService>();

        var booking = await bookingService
            .GetBookingByIdAsync(bookingId);

        var eventItem = await eventService
            .GetByIdAsync(eventId);

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.Equal(1, eventItem.AvailableSeats);
    }

    private static async Task<(Guid EventId, Guid BookingId)>
        CreatePendingBookingAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();

        var eventService = scope.ServiceProvider
            .GetRequiredService<IEventService>();

        var bookingService = scope.ServiceProvider
            .GetRequiredService<IBookingService>();

        var eventItem = await eventService.CreateAsync(
            new Event("Test event")
            {
                Description = "Test description",
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(2),
                TotalSeats = 1
            });

        var booking = await bookingService
            .CreateBookingAsync(eventItem.Id);

        return (eventItem.Id, booking.Id);
    }
}