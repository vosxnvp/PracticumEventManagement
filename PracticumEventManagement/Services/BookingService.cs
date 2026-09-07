using System.Collections.Concurrent;
using PracticumEventManagement.Dtos;
using PracticumEventManagement.Exceptions;
using PracticumEventManagement.Models;

namespace PracticumEventManagement.Services;

public class BookingService : IBookingService
{
    private readonly IEventService _eventService;

    private readonly ConcurrentDictionary<Guid, Booking> _bookings = new();

    public BookingService(IEventService eventService)
    {
        _eventService = eventService;
    }

    public Task<BookingInfo> CreateBookingAsync(Guid eventId)
    {
        _eventService.GetById(eventId);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null
        };

        _bookings[booking.Id] = booking;

        return Task.FromResult(ToBookingInfo(booking));
    }

    public Task<BookingInfo> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = GetBooking(bookingId);

        return Task.FromResult(ToBookingInfo(booking));
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

    public Task<IReadOnlyCollection<BookingInfo>> GetPendingBookingsAsync()
    {
        var pendingBookings = _bookings.Values
            .Where(booking => booking.Status == BookingStatus.Pending)
            .Select(ToBookingInfo)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<BookingInfo>>(pendingBookings);
    }

    public Task ConfirmBookingAsync(Guid bookingId)
    {
        var booking = GetBooking(bookingId);

        booking.Confirm();

        return Task.CompletedTask;
    }

    public Task RejectBookingAsync(Guid bookingId)
    {
        var booking = GetBooking(bookingId);

        booking.Reject();

        return Task.CompletedTask;
    }

    private Booking GetBooking(Guid bookingId)
    {
        if (!_bookings.TryGetValue(bookingId, out var booking))
        {
            throw new NotFoundException(
                $"Booking with id {bookingId} was not found.");
        }

        return booking;
    }
}