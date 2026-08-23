using PracticumEventManagement.Dtos;
using PracticumEventManagement.Models;
namespace PracticumEventManagement.Services;

public interface IEventService
{
  

    Event? GetById(Guid id);

    Event Create(Event eventItem);

    bool Update(Guid id, Event eventItem);

    bool Delete(Guid id);

    PaginatedResult<Event> GetAll(
    string? title = null,
    DateTime? from = null,
    DateTime? to = null,
    int page = 1,
    int pageSize = 10);
}

