
using PracticumEventManagement.DataAccess.Repositories;
using PracticumEventManagement.Dtos;
using PracticumEventManagement.Exceptions;
using PracticumEventManagement.Models;

namespace PracticumEventManagement.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;

    public EventService(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<Event> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _eventRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(
                $"Event with id {id} was not found.");
    }

    public async Task<Event> CreateAsync(
        Event eventItem,
        CancellationToken cancellationToken = default)
    {
        if (eventItem.EndAt <= eventItem.StartAt)
        {
            throw new ValidationException(
                "EndAt must be later than StartAt.");
        }

        if (eventItem.TotalSeats <= 0)
        {
            throw new ValidationException(
                "TotalSeats must be greater than zero.");
        }

        eventItem.Id = Guid.NewGuid();
        eventItem.AvailableSeats = eventItem.TotalSeats;

        await _eventRepository.AddAsync(eventItem, cancellationToken);
        await _eventRepository.SaveChangesAsync(cancellationToken);

        return eventItem;
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        Event eventItem,
        CancellationToken cancellationToken = default)
    {
        var existingEvent = await _eventRepository
            .GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(
                $"Event with id {id} was not found.");

        if (eventItem.EndAt <= eventItem.StartAt)
        {
            throw new ValidationException(
                "EndAt must be later than StartAt.");
        }

        existingEvent.Title = eventItem.Title;
        existingEvent.Description = eventItem.Description;
        existingEvent.StartAt = eventItem.StartAt;
        existingEvent.EndAt = eventItem.EndAt;

        await _eventRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var existingEvent = await _eventRepository
            .GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(
                $"Event with id {id} was not found.");

        _eventRepository.Remove(existingEvent);
        await _eventRepository.SaveChangesAsync(cancellationToken);

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
            throw new ValidationException(
                "page должен быть больше или равен 1");
        }

        if (pageSize < 1)
        {
            throw new ValidationException(
                "pageSize должен быть больше или равен 1.");
        }

        return await _eventRepository.GetAllAsync(
            title,
            from,
            to,
            page,
            pageSize,
            cancellationToken);
    }
}
