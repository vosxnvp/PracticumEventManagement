using Microsoft.Extensions.DependencyInjection;
using PracticumEventManagement.Application.Interfaces;

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

        var processor = scope.ServiceProvider
            .GetRequiredService<IBookingProcessor>();

        return await processor.GetPendingIdsAsync(cancellationToken);
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

            // Each booking gets its own scope and DbContext.
            using var scope = _scopeFactory.CreateScope();

            var processor = scope.ServiceProvider
                .GetRequiredService<IBookingProcessor>();

            var status = await processor.ProcessAsync(
                bookingId,
                cancellationToken);

            if (status is not null)
            {
                _logger.LogInformation(
                    "Booking {BookingId} processed with status {Status}",
                    bookingId,
                    status.Value);
            }
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

            // Recovery uses a fresh scope and DbContext.
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var processor = scope.ServiceProvider
                    .GetRequiredService<IBookingProcessor>();

                await processor.RejectAfterErrorAsync(
                    bookingId,
                    cancellationToken);
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