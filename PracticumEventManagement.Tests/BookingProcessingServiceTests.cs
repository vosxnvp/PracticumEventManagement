using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PracticumEventManagement.BackgroundServices;
using PracticumEventManagement.Dtos;
using PracticumEventManagement.Models;
using PracticumEventManagement.Services;
using PracticumEventManagement.Tests.Infrastructure;

namespace PracticumEventManagement.Tests;

public class BookingProcessingServiceTests
{
    [Fact]
    public async Task Processing_ShouldConfirmPendingBooking_WhenEventExists()
    {
        // Arrange: create an event and a pending booking in one scope.
        using var provider = TestDbFactory.CreateProvider();
        Guid bookingId;
        Guid eventId;

        using (var scope = provider.CreateScope())
        {
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var eventItem = await eventService.CreateAsync(new Event("Test event")
            {
                Description = "Test description",
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(2),
                TotalSeats = 1
            });

            eventId = eventItem.Id;
            var booking = await bookingService.CreateBookingAsync(eventId);
            bookingId = booking.Id;
        }

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var service = new TestBookingProcessingService(
            scopeFactory,
            NullLogger<BookingProcessingService>.Instance);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(12));
        var executionTask = service.RunAsync(cancellation.Token);

        try
        {
            // Act: read each status from a *new* scope, not from a tracked entity.
            BookingInfo current;
            do
            {
                await Task.Delay(100);
                using var checkScope = provider.CreateScope();
                var bookingService = checkScope.ServiceProvider
                    .GetRequiredService<IBookingService>();
                current = await bookingService.GetBookingByIdAsync(bookingId);
            }
            while (current.Status == BookingStatus.Pending &&
                   !cancellation.IsCancellationRequested);

            // Assert
            Assert.Equal(BookingStatus.Confirmed, current.Status);
            Assert.NotNull(current.ProcessedAt);

            using var eventScope = provider.CreateScope();
            var eventService = eventScope.ServiceProvider.GetRequiredService<IEventService>();
            var storedEvent = await eventService.GetByIdAsync(eventId);
            Assert.Equal(0, storedEvent.AvailableSeats);
        }
        finally
        {
            cancellation.Cancel();
            await executionTask;
        }
    }

    private sealed class TestBookingProcessingService : BookingProcessingService
    {
        public TestBookingProcessingService(
            IServiceScopeFactory scopeFactory,
            ILogger<BookingProcessingService> logger)
            : base(scopeFactory, logger)
        {
        }

        public Task RunAsync(CancellationToken cancellationToken)
            => ExecuteAsync(cancellationToken);
    }
}
