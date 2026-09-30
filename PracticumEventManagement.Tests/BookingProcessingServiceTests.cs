using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PracticumEventManagement.BackgroundServices;
using PracticumEventManagement.Dtos;
using PracticumEventManagement.Models;
using PracticumEventManagement.Services;

namespace PracticumEventManagement.Tests;

public class BookingProcessingServiceTests
{
    [Fact]
    public async Task Processing_ShouldRejectBooking_WhenEventWasDeleted()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = eventService.Create(new Event
        {
            Title = "Deleted event",
            Description = "Test event",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 1
        });

        var booking =
            await bookingService.CreateBookingAsync(eventItem.Id);

        eventService.Delete(eventItem.Id);

        var service = new TestBookingProcessingService(
            bookingService,
            eventService,
            NullLogger<BookingProcessingService>.Instance);

        using var cancellationTokenSource =
            new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        var executionTask =
            service.RunAsync(cancellationTokenSource.Token);

        BookingInfo bookingInfo;

        do
        {
            await Task.Delay(100);

            bookingInfo =
                await bookingService.GetBookingByIdAsync(
                    booking.Id);
        }
        while (
            bookingInfo.Status == BookingStatus.Pending &&
            !cancellationTokenSource.IsCancellationRequested);

        cancellationTokenSource.Cancel();

        await executionTask;

        // Assert
        Assert.Equal(
            BookingStatus.Rejected,
            bookingInfo.Status);

        Assert.NotNull(bookingInfo.ProcessedAt);
    }

    private sealed class TestBookingProcessingService
        : BookingProcessingService
    {
        public TestBookingProcessingService(
            IBookingService bookingService,
            IEventService eventService,
            ILogger<BookingProcessingService> logger)
            : base(
                bookingService,
                eventService,
                logger)
        {
        }

        public Task RunAsync(
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(cancellationToken);
        }
    }
}