
using PracticumEventManagement.Models;

namespace PracticumEventManagement.DataAccess.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Booking?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Booking>> GetPendingAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>> GetPendingIdsAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Booking booking,
        CancellationToken cancellationToken = default);

    void Remove(Booking booking);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
