using Microsoft.EntityFrameworkCore;
using PracticumEventManagement.Dtos;
using PracticumEventManagement.Models;

namespace PracticumEventManagement.DataAccess.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Event?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<PaginatedResult<Event>> GetAllAsync(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Event> query = _context.Events.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(title))
        {
            var normalizedTitle = title.ToLower();

            query = query.Where(e =>
                e.Title.ToLower().Contains(normalizedTitle));
        }

        if (from.HasValue)
        {
            query = query.Where(e => e.StartAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.EndAt <= to.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.StartAt)
            .ThenBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<Event>
        {
            TotalCount = totalCount,
            Items = items,
            Page = page,
            Count = items.Count
        };
    }

    public async Task AddAsync(
        Event eventItem,
        CancellationToken cancellationToken = default)
    {
        await _context.Events.AddAsync(eventItem, cancellationToken);
    }

    public void Remove(Event eventItem)
    {
        _context.Events.Remove(eventItem);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}