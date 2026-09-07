using PracticumEventManagement.Dtos;

namespace PracticumEventManagement.Services;

public interface IBookingService
{
    Task<BookingInfo> CreateBookingAsync(Guid eventId);

    Task<BookingInfo> GetBookingByIdAsync(Guid bookingId);

    Task<IReadOnlyCollection<BookingInfo>> GetPendingBookingsAsync();

    Task ConfirmBookingAsync(Guid bookingId);

    Task RejectBookingAsync(Guid bookingId);
}