using PracticumEventManagement.Application.Interfaces;
using PracticumEventManagement.Domain.Enums;

namespace PracticumEventManagement.Application.Services;

public class BookingProcessor : IBookingProcessor
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventRepository _eventRepository;

    public BookingProcessor(
        IBookingRepository bookingRepository,
        IEventRepository eventRepository)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
    }

    public async Task<IReadOnlyCollection<Guid>> GetPendingIdsAsync(
        CancellationToken cancellationToken)
    {
        return await _bookingRepository
            .GetPendingIdsAsync(cancellationToken);
    }

    public async Task<BookingStatus?> ProcessAsync(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository
            .GetTrackedByIdAsync(bookingId, cancellationToken);

        if (booking is null ||
            booking.Status != BookingStatus.Pending)
        {
            return null;
        }

        var eventItem = await _eventRepository
            .GetByIdAsync(booking.EventId, cancellationToken);

        if (eventItem is not null)
        {
            booking.Confirm();
        }
        else
        {
            booking.Reject();
        }

        await _bookingRepository
            .SaveChangesAsync(cancellationToken);

        return booking.Status;
    }

    public async Task RejectAfterErrorAsync(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository
            .GetTrackedByIdAsync(bookingId, cancellationToken);

        if (booking is null ||
            booking.Status != BookingStatus.Pending)
        {
            return;
        }

        var eventItem = await _eventRepository
            .GetTrackedByIdAsync(booking.EventId, cancellationToken);

        if (eventItem is not null)
        {
            eventItem.ReleaseSeats();
        }

        booking.Reject();

        await _bookingRepository
            .SaveChangesAsync(cancellationToken);
    }
}