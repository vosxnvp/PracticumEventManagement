using PracticumEventManagement.Services;

namespace PracticumEventManagement.BackgroundServices;

public class BookingProcessingService : BackgroundService
{
    private static readonly TimeSpan ProcessingDelay =
        TimeSpan.FromSeconds(2);

    private static readonly TimeSpan PollingInterval =
        TimeSpan.FromSeconds(1);

    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingProcessingService> _logger;

    public BookingProcessingService(
        IBookingService bookingService,
        ILogger<BookingProcessingService> logger)
    {
        _bookingService = bookingService;
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

                foreach (var booking in pendingBookings)
                {
                    _logger.LogInformation(
                        "Processing booking {BookingId}",
                        booking.Id);

                    await Task.Delay(
                        ProcessingDelay,
                        stoppingToken);

                    await _bookingService.ConfirmBookingAsync(
                        booking.Id);

                    _logger.LogInformation(
                        "Booking {BookingId} confirmed",
                        booking.Id);
                }

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
}