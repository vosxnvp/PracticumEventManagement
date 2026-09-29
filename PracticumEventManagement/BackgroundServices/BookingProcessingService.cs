using PracticumEventManagement.Dtos;
using PracticumEventManagement.Services;

namespace PracticumEventManagement.BackgroundServices;

public class BookingProcessingService : BackgroundService
{
    private static readonly TimeSpan ProcessingDelay =
        TimeSpan.FromSeconds(2);

    private static readonly TimeSpan PollingInterval =
        TimeSpan.FromSeconds(1);

    private readonly IBookingService _bookingService;
    private readonly IEventService _eventService;
    private readonly ILogger<BookingProcessingService> _logger;
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    public BookingProcessingService(
        IBookingService bookingService,
        IEventService eventService,
        ILogger<BookingProcessingService> logger)
    {
        _bookingService = bookingService;
        _eventService = eventService;
        _logger = logger;
    }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var pendingBookings =
                    await _bookingService.GetPendingBookingsAsync();

                var tasks = pendingBookings.Select(
                    booking => ProcessBookingAsync(
                        booking,
                        stoppingToken));

                await Task.WhenAll(tasks);

                await Task.Delay(
                    PollingInterval,
                    stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Booking processing service stopped.");
        }
    }

    private async Task ProcessBookingAsync(
        BookingInfo booking,
        CancellationToken stoppingToken)
    {
        var confirmed = false;

        try
        {
            _logger.LogInformation(
                "Processing booking {BookingId}",
                booking.Id);

            await Task.Delay(
                ProcessingDelay,
                stoppingToken);

            await _processingSemaphore.WaitAsync(stoppingToken);

            try
            {
                var eventItem =
                    _eventService.GetById(booking.EventId);

                if (eventItem is null)
                {
                    await _bookingService.RejectBookingAsync(
                        booking.Id);

                    _logger.LogWarning(
                        "Booking {BookingId} rejected because event {EventId} was not found",
                        booking.Id,
                        booking.EventId);

                    return;
                }

                await _bookingService.ConfirmBookingAsync(
                    booking.Id);

                confirmed = true;

                _logger.LogInformation(
                    "Booking {BookingId} confirmed",
                    booking.Id);
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Error while processing booking {BookingId}",
                booking.Id);

            if (!confirmed)
            {
                await _processingSemaphore.WaitAsync();

                try
                {
                    var eventItem =
                        _eventService.GetById(booking.EventId);

                    if (eventItem is not null)
                    {
                        eventItem.ReleaseSeats();
                    }

                    await _bookingService.RejectBookingAsync(
                        booking.Id);
                }
                finally
                {
                    _processingSemaphore.Release();
                }
            }
        }
    }
}
