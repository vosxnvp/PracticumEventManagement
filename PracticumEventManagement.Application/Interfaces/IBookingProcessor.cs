using PracticumEventManagement.Domain.Enums;

namespace PracticumEventManagement.Application.Interfaces;

public interface IBookingProcessor
{
    Task<IReadOnlyCollection<Guid>> GetPendingIdsAsync(
        CancellationToken cancellationToken);

    Task<BookingStatus?> ProcessAsync(
        Guid bookingId,
        CancellationToken cancellationToken);

    Task RejectAfterErrorAsync(
        Guid bookingId,
        CancellationToken cancellationToken);
}