using PracticumEventManagement.Dtos;
using PracticumEventManagement.Models;

namespace PracticumEventManagement.DataAccess.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Event?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PaginatedResult<Event>> GetAllAsync(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Event eventItem,
        CancellationToken cancellationToken = default);

    void Remove(Event eventItem);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}