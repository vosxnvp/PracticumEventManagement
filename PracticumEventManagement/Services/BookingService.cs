
using PracticumEventManagement.DataAccess.Repositories;
using PracticumEventManagement.Dtos;
using PracticumEventManagement.Exceptions;
using PracticumEventManagement.Models;

namespace PracticumEventManagement.Services;

public class BookingService : IBookingService
{
    private static readonly SemaphoreSlim BookingSemaphore = new(1, 1);

    private readonly IEventRepository _eventRepository;
    private readonly IBookingRepository _bookingRepository;

    public BookingService(
        IEventRepository eventRepository,
        IBookingRepository bookingRepository)
    {
        _eventRepository = eventRepository;
        _bookingRepository = bookingRepository;
    }

    public async Task<BookingInfo> CreateBookingAsync(Guid eventId)
    {
        await BookingSemaphore.WaitAsync();

        try
        {
            var eventItem = await _eventRepository
                .GetTrackedByIdAsync(eventId)
                ?? throw new NotFoundException(
                    $"Event with id {eventId} was not found.");

            if (!eventItem.TryReserveSeats())
            {
                throw new NoAvailableSeatsException(
                    "No available seats for this event");
            }

            var booking = new Booking(eventId)
            {
                Id = Guid.NewGuid(),
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = null
            };

            await _bookingRepository.AddAsync(booking);

            // Save event and booking changes in one transaction.
            await _bookingRepository.SaveChangesAsync();

            return ToBookingInfo(booking);
        }
        finally
        {
            BookingSemaphore.Release();
        }
    }

    public async Task<BookingInfo> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _bookingRepository
            .GetByIdAsync(bookingId)
            ?? throw new NotFoundException(
                $"Booking with id {bookingId} was not found.");

        return ToBookingInfo(booking);
    }

    public async Task<IReadOnlyCollection<BookingInfo>> GetPendingBookingsAsync()
    {
        var pendingBookings = await _bookingRepository.GetPendingAsync();

        return pendingBookings
            .Select(ToBookingInfo)
            .ToArray();
    }

    public async Task ConfirmBookingAsync(Guid bookingId)
    {
        var booking = await GetTrackedBookingAsync(bookingId);

        booking.Confirm();

        await _bookingRepository.SaveChangesAsync();
    }


    public async Task RejectBookingAsync(Guid bookingId)
    {
        var booking = await GetTrackedBookingAsync(bookingId);

        if (booking.Status != BookingStatus.Pending)
        {
            return;
        }

        var eventItem = await _eventRepository
            .GetTrackedByIdAsync(booking.EventId);

        booking.Reject();

        if (eventItem is not null)
        {
            eventItem.ReleaseSeats();
        }

        // Both entities are saved through one scoped DbContext.
        await _bookingRepository.SaveChangesAsync();
    }


    private async Task<Booking> GetTrackedBookingAsync(Guid bookingId)
    {
        return await _bookingRepository
            .GetTrackedByIdAsync(bookingId)
            ?? throw new NotFoundException(
                $"Booking with id {bookingId} was not found.");
    }

    private static BookingInfo ToBookingInfo(Booking booking)
    {
        return new BookingInfo
        {
            Id = booking.Id,
            EventId = booking.EventId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };
    }
}
