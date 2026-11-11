
using Microsoft.EntityFrameworkCore;
using PracticumEventManagement.Models;

namespace PracticumEventManagement.DataAccess.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.Id == id,
                cancellationToken);
    }

    public async Task<Booking?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .FirstOrDefaultAsync(
                b => b.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<Booking>> GetPendingAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingStatus.Pending)
            .OrderBy(b => b.CreatedAt)
            .ThenBy(b => b.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> GetPendingIdsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingStatus.Pending)
            .OrderBy(b => b.CreatedAt)
            .ThenBy(b => b.Id)
            .Select(b => b.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(
        Booking booking,
        CancellationToken cancellationToken = default)
    {
        await _context.Bookings.AddAsync(
            booking,
            cancellationToken);
    }

    public void Remove(Booking booking)
    {
        _context.Bookings.Remove(booking);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
