using Microsoft.EntityFrameworkCore;
using PracticumEventManagement.DataAccess;
using PracticumEventManagement.Dtos;
using PracticumEventManagement.Exceptions;
using PracticumEventManagement.Models;

namespace PracticumEventManagement.Services;

public class EventService : IEventService
{
    private readonly AppDbContext _context;

    public EventService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Event> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Event with id {id} was not found.");
    }

    public async Task<Event> CreateAsync(
        Event eventItem,
        CancellationToken cancellationToken = default)
    {
        if (eventItem.EndAt <= eventItem.StartAt)
        {
            throw new ValidationException("EndAt must be later than StartAt.");
        }

        if (eventItem.TotalSeats <= 0)
        {
            throw new ValidationException("TotalSeats must be greater than zero.");
        }

        eventItem.Id = Guid.NewGuid();
        eventItem.AvailableSeats = eventItem.TotalSeats;

        _context.Events.Add(eventItem);
        await _context.SaveChangesAsync(cancellationToken);

        return eventItem;
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        Event eventItem,
        CancellationToken cancellationToken = default)
    {
        var existingEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Event with id {id} was not found.");

        if (eventItem.EndAt <= eventItem.StartAt)
        {
            throw new ValidationException("EndAt must be later than StartAt.");
        }

        existingEvent.Title = eventItem.Title;
        existingEvent.Description = eventItem.Description;
        existingEvent.StartAt = eventItem.StartAt;
        existingEvent.EndAt = eventItem.EndAt;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var existingEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Event with id {id} was not found.");

        _context.Events.Remove(existingEvent);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PaginatedResult<Event>> GetAllAsync(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new ValidationException("page должен быть больше или равен 1");
        }

        if (pageSize < 1)
        {
            throw new ValidationException("pageSize должен быть больше или равен 1.");
        }

        IQueryable<Event> query = _context.Events.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(title))
        {
            var normalizedTitle = title.ToLower();
            query = query.Where(e => e.Title.ToLower().Contains(normalizedTitle));
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
}
