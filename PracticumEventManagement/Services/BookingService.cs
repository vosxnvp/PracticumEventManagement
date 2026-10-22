using Microsoft.EntityFrameworkCore;
using PracticumEventManagement.DataAccess;
using PracticumEventManagement.Dtos;
using PracticumEventManagement.Exceptions;
using PracticumEventManagement.Models;

namespace PracticumEventManagement.Services;

public class BookingService : IBookingService
{
    // Shared across all scoped instances in this application process.
    private static readonly SemaphoreSlim BookingSemaphore = new(1, 1);

    private readonly AppDbContext _context;

    public BookingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BookingInfo> CreateBookingAsync(Guid eventId)
    {
        await BookingSemaphore.WaitAsync();

        try
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventId)
                ?? throw new NotFoundException($"Event with id {eventId} was not found.");

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

            _context.Bookings.Add(booking);

            // Both the new booking and the reserved seat are saved together.
            await _context.SaveChangesAsync();

            return ToBookingInfo(booking);
        }
        finally
        {
            BookingSemaphore.Release();
        }
    }

    public async Task<BookingInfo> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new NotFoundException(
                $"Booking with id {bookingId} was not found.");

        return ToBookingInfo(booking);
    }

    public async Task<IReadOnlyCollection<BookingInfo>> GetPendingBookingsAsync()
    {
        var pendingBookings = await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingStatus.Pending)
            .OrderBy(b => b.CreatedAt)
            .Select(b => new BookingInfo
            {
                Id = b.Id,
                EventId = b.EventId,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                ProcessedAt = b.ProcessedAt
            })
            .ToArrayAsync();

        return pendingBookings;
    }

    public async Task ConfirmBookingAsync(Guid bookingId)
    {
        var booking = await GetBookingAsync(bookingId);
        booking.Confirm();
        await _context.SaveChangesAsync();
    }

    public async Task RejectBookingAsync(Guid bookingId)
    {
        var booking = await GetBookingAsync(bookingId);
        booking.Reject();
        await _context.SaveChangesAsync();
    }

    private async Task<Booking> GetBookingAsync(Guid bookingId)
    {
        return await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId)
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
