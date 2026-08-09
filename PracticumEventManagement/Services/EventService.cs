using PracticumEventManagement.Models;

namespace PracticumEventManagement.Services;

public class EventService : IEventService
{
    private readonly List<Event> _events = new();

    public IEnumerable<Event> GetAll()
    {
        return _events;
    }

    public Event? GetById(Guid id)
    {
        return _events.FirstOrDefault(e => e.Id == id);
    }

    public Event Create(Event eventItem)
    {
        eventItem.Id = Guid.NewGuid();
        _events.Add(eventItem);

        return eventItem;
    }

    public bool Update(Guid id, Event eventItem)
    {
        var existingEvent = GetById(id);

        if (existingEvent is null)
        {
            return false;
        }

        existingEvent.Title = eventItem.Title;
        existingEvent.Description = eventItem.Description;
        existingEvent.StartAt = eventItem.StartAt;
        existingEvent.EndAt = eventItem.EndAt;

        return true;
    }

    public bool Delete(Guid id)
    {
        var existingEvent = GetById(id);

        if (existingEvent is null)
        {
            return false;
        }

        _events.Remove(existingEvent);

        return true;
    }
}