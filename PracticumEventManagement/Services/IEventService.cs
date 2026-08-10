using PracticumEventManagement.Models;
namespace PracticumEventManagement.Services;

public interface IEventService
{
    IEnumerable<Event> GetAll();

    Event? GetById(Guid id);

    Event Create(Event eventItem);

    bool Update(Guid id, Event eventItem);

    bool Delete(Guid id);
}

