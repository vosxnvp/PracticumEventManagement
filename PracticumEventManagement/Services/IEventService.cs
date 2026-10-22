using PracticumEventManagement.Dtos;
using PracticumEventManagement.Models;

namespace PracticumEventManagement.Services;

public interface IEventService
{
    Task<Event> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Event> CreateAsync(Event eventItem, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Guid id, Event eventItem, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PaginatedResult<Event>> GetAllAsync(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
}
