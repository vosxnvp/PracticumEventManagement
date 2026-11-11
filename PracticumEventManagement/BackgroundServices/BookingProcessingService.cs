
using Microsoft.Extensions.DependencyInjection;
using PracticumEventManagement.DataAccess.Repositories;
using PracticumEventManagement.Models;

namespace PracticumEventManagement.BackgroundServices;

public class BookingProcessingService : BackgroundService
{
    private static readonly TimeSpan ProcessingDelay =
        TimeSpan.FromSeconds(2);

    private static readonly TimeSpan PollingInterval =
        TimeSpan.FromSeconds(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingProcessingService> _logger;

    public BookingProcessingService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingProcessingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pendingIds =
                    await GetPendingBookingIdsAsync(stoppingToken);

                var tasks = pendingIds.Select(
                    id => ProcessBookingAsync(id, stoppingToken));

                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unexpected error while processing booking batch");
            }

            try
            {
                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation(
            "Booking processing service stopped.");
    }

    private async Task<IReadOnlyCollection<Guid>> GetPendingBookingIdsAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var bookingRepository = scope.ServiceProvider
            .GetRequiredService<IBookingRepository>();

        return await bookingRepository
            .GetPendingIdsAsync(cancellationToken);
    }

    private async Task ProcessBookingAsync(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Processing booking {BookingId}",
                bookingId);

            await Task.Delay(
                ProcessingDelay,
                cancellationToken);

            // Each task gets its own scope and DbContext.
            using var scope = _scopeFactory.CreateScope();

            var bookingRepository = scope.ServiceProvider
                .GetRequiredService<IBookingRepository>();

            var eventRepository = scope.ServiceProvider
                .GetRequiredService<IEventRepository>();

            var booking = await bookingRepository
                .GetTrackedByIdAsync(
                    bookingId,
                    cancellationToken);

            if (booking is null ||
                booking.Status != BookingStatus.Pending)
            {
                return;
            }

            var eventItem = await eventRepository
                .GetByIdAsync(
                    booking.EventId,
                    cancellationToken);

            if (eventItem is not null)
            {
                booking.Confirm();
            }
            else
            {
                booking.Reject();
            }

            await bookingRepository
                .SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Booking {BookingId} processed with status {Status}",
                booking.Id,
                booking.Status);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Error while processing booking {BookingId}",
                bookingId);

            // Recover using a fresh scope.
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var bookingRepository = scope.ServiceProvider
                    .GetRequiredService<IBookingRepository>();

                var eventRepository = scope.ServiceProvider
                    .GetRequiredService<IEventRepository>();

                var booking = await bookingRepository
                    .GetTrackedByIdAsync(
                        bookingId,
                        cancellationToken);

                if (booking is null ||
                    booking.Status != BookingStatus.Pending)
                {
                    return;
                }

                var eventItem = await eventRepository
                    .GetTrackedByIdAsync(
                        booking.EventId,
                        cancellationToken);

                if (eventItem is not null)
                {
                    eventItem.ReleaseSeats();
                }

                booking.Reject();

                // Both repositories share one scoped DbContext.
                await bookingRepository
                    .SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception recoveryException)
            {
                _logger.LogError(
                    recoveryException,
                    "Failed to reject booking {BookingId} after processing error",
                    bookingId);
            }
        }
    }
}
