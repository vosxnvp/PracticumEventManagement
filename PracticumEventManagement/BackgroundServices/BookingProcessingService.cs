using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PracticumEventManagement.DataAccess;
using PracticumEventManagement.Models;

namespace PracticumEventManagement.BackgroundServices;

public class BookingProcessingService : BackgroundService
{
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingProcessingService> _logger;

    public BookingProcessingService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingProcessingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pendingIds = await GetPendingBookingIdsAsync(stoppingToken);
                var tasks = pendingIds.Select(id => ProcessBookingAsync(id, stoppingToken));
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
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
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Booking processing service stopped.");
    }

    private async Task<List<Guid>> GetPendingBookingIdsAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await context.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingStatus.Pending)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task ProcessBookingAsync(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing booking {BookingId}", bookingId);
            await Task.Delay(ProcessingDelay, cancellationToken);

            // Each concurrent booking task gets its own DbContext.
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var booking = await context.Bookings
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking is null || booking.Status != BookingStatus.Pending)
            {
                return;
            }

            var eventExists = await context.Events
                .AnyAsync(e => e.Id == booking.EventId, cancellationToken);

            if (eventExists)
            {
                booking.Confirm();
            }
            else
            {
                booking.Reject();
            }

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Booking {BookingId} processed with status {Status}",
                booking.Id,
                booking.Status);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Error while processing booking {BookingId}",
                bookingId);

            // Recover in a fresh scope, since the previous DbContext may be unusable.
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var booking = await context.Bookings
                    .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

                if (booking is null || booking.Status != BookingStatus.Pending)
                {
                    return;
                }

                var eventItem = await context.Events
                    .FirstOrDefaultAsync(e => e.Id == booking.EventId, cancellationToken);

                if (eventItem is not null)
                {
                    eventItem.ReleaseSeats();
                }

                booking.Reject();
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
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
